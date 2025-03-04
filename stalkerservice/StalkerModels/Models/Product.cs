namespace StalkerModels.Models;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MainImage { get; set; }
    public string? Images { get; set; } // JSON array of image URLs
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    public string? Specifications { get; set; } // JSON object of product specifications
    public string? Tags { get; set; } // Comma-separated tags
    public decimal Weight { get; set; }
    public string? Dimensions { get; set; } // JSON object (length, width, height)
    public bool IsFeatured { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;
    public virtual ICollection<ProductPrice> Prices { get; set; } = new List<ProductPrice>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<ResellerProduct> ResellerProducts { get; set; } = new List<ResellerProduct>();
}
