using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StalkerApi.Repositories;
using StalkerModels.Models;

namespace StalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] ProductQueryParameters parameters)
    {
        try
        {
            var query = (await _productRepository.GetAllAsync())
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                query = query.Where(p => 
                    p.Name.Contains(parameters.SearchTerm) || 
                    p.Description.Contains(parameters.SearchTerm) ||
                    p.SKU.Contains(parameters.SearchTerm));
            }

            if (parameters.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == parameters.CategoryId);
            }

            if (parameters.MinPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice >= parameters.MinPrice);
            }

            if (parameters.MaxPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice <= parameters.MaxPrice);
            }

            // Apply sorting
            query = parameters.SortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.BasePrice),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // Apply pagination
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)parameters.PageSize);

            var items = query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();

            var response = new PagedResponse<Product>
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
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, "Internal server error while retrieving products");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {Id}", id);
            return StatusCode(500, "Internal server error while retrieving the product");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify category exists
            var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
            if (category == null)
                return BadRequest("Invalid category ID");

            var created = await _productRepository.AddAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "Internal server error while creating the product");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
    {
        try
        {
            if (id != product.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify category exists
            var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
            if (category == null)
                return BadRequest("Invalid category ID");

            await _productRepository.UpdateAsync(product);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return StatusCode(500, "Internal server error while updating the product");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            await _productRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            return StatusCode(500, "Internal server error while deleting the product");
        }
    }
}

public class ProductQueryParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(value, MaxPageSize);
    }
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}
