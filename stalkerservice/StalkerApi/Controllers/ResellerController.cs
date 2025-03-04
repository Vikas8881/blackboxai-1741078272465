using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StalkerApi.Repositories;
using StalkerModels.Models;

namespace StalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Reseller")]
public class ResellerController : ControllerBase
{
    private readonly IRepository<ResellerProduct> _resellerProductRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ResellerController> _logger;

    public ResellerController(
        IRepository<ResellerProduct> resellerProductRepository,
        IRepository<Product> productRepository,
        UserManager<User> userManager,
        ILogger<ResellerController> logger)
    {
        _resellerProductRepository = resellerProductRepository;
        _productRepository = productRepository;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<ResellerProduct>>> GetResellerProducts()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var products = (await _resellerProductRepository.GetAllAsync())
                .Where(rp => rp.ResellerId == userId)
                .OrderByDescending(rp => rp.CreatedAt)
                .ToList();

            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reseller products");
            return StatusCode(500, "Internal server error while retrieving reseller products");
        }
    }

    [HttpPost("products/{productId}")]
    public async Task<ActionResult<ResellerProduct>> AddResellerProduct(int productId, [FromBody] ResellerProductRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Verify product exists
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return NotFound("Product not found");

            // Check if already added
            var existing = (await _resellerProductRepository.GetAllAsync())
                .FirstOrDefault(rp => rp.ResellerId == userId && rp.ProductId == productId);

            if (existing != null)
                return BadRequest("Product already added to reseller catalog");

            var resellerProduct = new ResellerProduct
            {
                ResellerId = userId,
                ProductId = productId,
                CommissionRate = request.CommissionRate,
                CustomPrice = request.CustomPrice,
                Notes = request.Notes,
                IsActive = true
            };

            var created = await _resellerProductRepository.AddAsync(resellerProduct);
            return CreatedAtAction(nameof(GetResellerProduct), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reseller product");
            return StatusCode(500, "Internal server error while adding reseller product");
        }
    }

    [HttpGet("products/{id}")]
    public async Task<ActionResult<ResellerProduct>> GetResellerProduct(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var resellerProduct = await _resellerProductRepository.GetByIdAsync(id);
            if (resellerProduct == null || resellerProduct.ResellerId != userId)
                return NotFound();

            return Ok(resellerProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reseller product {Id}", id);
            return StatusCode(500, "Internal server error while retrieving reseller product");
        }
    }

    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateResellerProduct(int id, [FromBody] ResellerProductRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var resellerProduct = await _resellerProductRepository.GetByIdAsync(id);
            if (resellerProduct == null || resellerProduct.ResellerId != userId)
                return NotFound();

            resellerProduct.CommissionRate = request.CommissionRate;
            resellerProduct.CustomPrice = request.CustomPrice;
            resellerProduct.Notes = request.Notes;

            await _resellerProductRepository.UpdateAsync(resellerProduct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reseller product {Id}", id);
            return StatusCode(500, "Internal server error while updating reseller product");
        }
    }

    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteResellerProduct(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var resellerProduct = await _resellerProductRepository.GetByIdAsync(id);
            if (resellerProduct == null || resellerProduct.ResellerId != userId)
                return NotFound();

            await _resellerProductRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reseller product {Id}", id);
            return StatusCode(500, "Internal server error while deleting reseller product");
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ResellerStats>> GetResellerStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // TODO: Implement statistics calculation from OrderItems
            var stats = new ResellerStats
            {
                TotalSales = 0,
                TotalCommission = 0,
                TotalOrders = 0,
                TopProducts = new List<ResellerProductStats>()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reseller stats");
            return StatusCode(500, "Internal server error while retrieving reseller stats");
        }
    }
}

public class ResellerProductRequest
{
    [Required]
    [Range(0, 100)]
    public decimal CommissionRate { get; set; }

    public decimal? CustomPrice { get; set; }
    public string? Notes { get; set; }
}

public class ResellerStats
{
    public decimal TotalSales { get; set; }
    public decimal TotalCommission { get; set; }
    public int TotalOrders { get; set; }
    public List<ResellerProductStats> TopProducts { get; set; } = new();
}

public class ResellerProductStats
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCommission { get; set; }
}
