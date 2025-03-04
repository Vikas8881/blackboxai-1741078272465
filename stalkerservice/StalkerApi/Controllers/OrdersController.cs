using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StalkerApi.Repositories;
using StalkerModels.Models;

namespace StalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<ResellerProduct> _resellerProductRepository;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IRepository<Order> orderRepository,
        IRepository<Product> productRepository,
        IRepository<ResellerProduct> resellerProductRepository,
        UserManager<User> userManager,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _resellerProductRepository = resellerProductRepository;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders([FromQuery] OrderQueryParameters parameters)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = (await _orderRepository.GetAllAsync()).AsQueryable();

            // Filter based on user role
            if (!User.IsInRole("Admin"))
            {
                if (User.IsInRole("Reseller"))
                {
                    query = query.Where(o => o.ResellerId == userId);
                }
                else
                {
                    query = query.Where(o => o.UserId == userId);
                }
            }

            // Apply filters
            if (!string.IsNullOrEmpty(parameters.OrderNumber))
            {
                query = query.Where(o => o.OrderNumber.Contains(parameters.OrderNumber));
            }

            if (parameters.Status.HasValue)
            {
                query = query.Where(o => o.Status == parameters.Status);
            }

            if (parameters.StartDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= parameters.StartDate);
            }

            if (parameters.EndDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= parameters.EndDate);
            }

            // Apply sorting
            query = parameters.SortBy?.ToLower() switch
            {
                "date_asc" => query.OrderBy(o => o.CreatedAt),
                "date_desc" => query.OrderByDescending(o => o.CreatedAt),
                "total_asc" => query.OrderBy(o => o.Total),
                "total_desc" => query.OrderByDescending(o => o.Total),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            // Apply pagination
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)parameters.PageSize);

            var items = query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();

            var response = new PagedResponse<Order>
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = parameters.Page,
                PageSize = parameters.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, "Internal server error while retrieving orders");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                return NotFound();

            // Verify access rights
            if (!User.IsInRole("Admin") && order.UserId != userId && order.ResellerId != userId)
                return Forbid();

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {Id}", id);
            return StatusCode(500, "Internal server error while retrieving the order");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] OrderRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Validate items
            if (!request.Items.Any())
                return BadRequest("Order must contain at least one item");

            // Create order
            var order = new Order
            {
                UserId = userId,
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = request.PaymentMethod,
                CurrencyCode = request.CurrencyCode ?? "USD",
                CurrencyRate = 1, // TODO: Get actual currency rate

                // Shipping information
                ShippingName = request.ShippingName,
                ShippingAddress = request.ShippingAddress,
                ShippingCity = request.ShippingCity,
                ShippingState = request.ShippingState,
                ShippingCountry = request.ShippingCountry,
                ShippingPostalCode = request.ShippingPostalCode,
                ShippingPhone = request.ShippingPhone,
                ShippingEmail = request.ShippingEmail
            };

            // Process items
            decimal subtotal = 0;
            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    return BadRequest($"Product not found: {item.ProductId}");

                if (!product.IsActive)
                    return BadRequest($"Product is not available: {item.ProductId}");

                if (product.StockQuantity < item.Quantity)
                    return BadRequest($"Insufficient stock for product: {item.ProductId}");

                // Check for reseller
                ResellerProduct? resellerProduct = null;
                if (!string.IsNullOrEmpty(request.ResellerId))
                {
                    resellerProduct = (await _resellerProductRepository.GetAllAsync())
                        .FirstOrDefault(rp => rp.ResellerId == request.ResellerId && 
                                            rp.ProductId == product.Id && 
                                            rp.IsActive);
                    
                    if (resellerProduct == null)
                        return BadRequest($"Product not available from specified reseller: {item.ProductId}");
                }

                var unitPrice = resellerProduct?.CustomPrice ?? product.BasePrice;
                var discountedPrice = product.DiscountedPrice;
                var finalPrice = discountedPrice ?? unitPrice;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    DiscountedUnitPrice = discountedPrice,
                    SubTotal = finalPrice * item.Quantity,
                    Total = finalPrice * item.Quantity, // Will be updated after applying discounts
                    ResellerId = request.ResellerId,
                    ResellerCommissionRate = resellerProduct?.CommissionRate
                };

                order.Items.Add(orderItem);
                subtotal += orderItem.SubTotal;
            }

            // Calculate totals
            order.SubTotal = subtotal;
            order.Tax = subtotal * 0.1m; // TODO: Implement proper tax calculation
            order.ShippingCost = 10; // TODO: Implement proper shipping calculation
            order.Total = order.SubTotal + order.Tax + order.ShippingCost;

            // Save order
            var created = await _orderRepository.AddAsync(order);
            
            // TODO: Update product stock quantities
            // TODO: Process payment
            // TODO: Send confirmation emails

            return CreatedAtAction(nameof(GetOrder), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, "Internal server error while creating the order");
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdate request)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                return NotFound();

            order.Status = request.Status;
            if (request.Status == OrderStatus.Delivered)
            {
                // Update payment status if not already completed
                if (order.PaymentStatus == PaymentStatus.Pending)
                {
                    order.PaymentStatus = PaymentStatus.Completed;
                    order.PaymentDate = DateTime.UtcNow;
                }
            }

            await _orderRepository.UpdateAsync(order);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status {Id}", id);
            return StatusCode(500, "Internal server error while updating the order status");
        }
    }

    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}

public class OrderQueryParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(value, MaxPageSize);
    }
    public string? OrderNumber { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; }
}

public class OrderRequest
{
    [Required]
    public List<OrderItemRequest> Items { get; set; } = new();
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    public string? ResellerId { get; set; }
    public string? CurrencyCode { get; set; }

    [Required]
    public string ShippingName { get; set; } = string.Empty;
    
    [Required]
    public string ShippingAddress { get; set; } = string.Empty;
    
    [Required]
    public string ShippingCity { get; set; } = string.Empty;
    
    [Required]
    public string ShippingState { get; set; } = string.Empty;
    
    [Required]
    public string ShippingCountry { get; set; } = string.Empty;
    
    [Required]
    public string ShippingPostalCode { get; set; } = string.Empty;
    
    [Required]
    public string ShippingPhone { get; set; } = string.Empty;
    
    [EmailAddress]
    public string? ShippingEmail { get; set; }
}

public class OrderItemRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class OrderStatusUpdate
{
    [Required]
    public OrderStatus Status { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}
