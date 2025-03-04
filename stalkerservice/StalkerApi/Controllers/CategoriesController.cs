using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StalkerApi.Repositories;
using StalkerModels.Models;

namespace StalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        IRepository<Category> categoryRepository,
        ILogger<CategoriesController> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] bool includeInactive = false)
    {
        try
        {
            var categories = (await _categoryRepository.GetAllAsync())
                .Where(c => includeInactive || c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            // Organize into hierarchy
            var hierarchicalCategories = categories
                .Where(c => c.ParentCategoryId == null)
                .Select(c => BuildCategoryHierarchy(c, categories))
                .ToList();

            return Ok(hierarchicalCategories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, "Internal server error while retrieving categories");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {Id}", id);
            return StatusCode(500, "Internal server error while retrieving the category");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] Category category)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify parent category if specified
            if (category.ParentCategoryId.HasValue)
            {
                var parentCategory = await _categoryRepository.GetByIdAsync(category.ParentCategoryId.Value);
                if (parentCategory == null)
                    return BadRequest("Invalid parent category ID");
            }

            // Generate slug if not provided
            if (string.IsNullOrEmpty(category.Slug))
            {
                category.Slug = GenerateSlug(category.Name);
            }

            var created = await _categoryRepository.AddAsync(category);
            return CreatedAtAction(nameof(GetCategory), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, "Internal server error while creating the category");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
    {
        try
        {
            if (id != category.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify parent category if specified
            if (category.ParentCategoryId.HasValue)
            {
                if (category.ParentCategoryId.Value == id)
                    return BadRequest("Category cannot be its own parent");

                var parentCategory = await _categoryRepository.GetByIdAsync(category.ParentCategoryId.Value);
                if (parentCategory == null)
                    return BadRequest("Invalid parent category ID");

                // Prevent circular references
                var currentParent = parentCategory;
                while (currentParent != null)
                {
                    if (currentParent.Id == id)
                        return BadRequest("Circular reference detected in category hierarchy");

                    currentParent = currentParent.ParentCategory;
                }
            }

            // Update slug if name changed and slug not explicitly provided
            if (string.IsNullOrEmpty(category.Slug))
            {
                category.Slug = GenerateSlug(category.Name);
            }

            await _categoryRepository.UpdateAsync(category);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return StatusCode(500, "Internal server error while updating the category");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            // Check if category has subcategories
            if (category.SubCategories.Any())
                return BadRequest("Cannot delete category with subcategories");

            // Check if category has products
            if (category.Products.Any())
                return BadRequest("Cannot delete category with products");

            await _categoryRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return StatusCode(500, "Internal server error while deleting the category");
        }
    }

    private Category BuildCategoryHierarchy(Category category, List<Category> allCategories)
    {
        var hierarchicalCategory = new Category
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Slug = category.Slug,
            Image = category.Image,
            ParentCategoryId = category.ParentCategoryId,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            MetaTitle = category.MetaTitle,
            MetaDescription = category.MetaDescription,
            MetaKeywords = category.MetaKeywords,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        var children = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => BuildCategoryHierarchy(c, allCategories))
            .ToList();

        hierarchicalCategory.SubCategories = children;
        return hierarchicalCategory;
    }

    private string GenerateSlug(string name)
    {
        // Convert to lowercase and replace spaces with hyphens
        var slug = name.ToLower().Replace(" ", "-");
        
        // Remove special characters
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        
        // Remove multiple consecutive hyphens
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }
        
        return slug.Trim('-');
    }
}
