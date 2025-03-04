namespace StalkerModels.Models;

public class ProductPrice : BaseEntity
{
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string CurrencyCode { get; set; } = "USD";
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
}

public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Format { get; set; } // Example: {symbol}{price} or {price}{symbol}
    public virtual ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();
}

public class ResellerProduct : BaseEntity
{
    public string ResellerId { get; set; } = string.Empty;
    public virtual User Reseller { get; set; } = null!;
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? CustomPrice { get; set; }
    public string? Notes { get; set; }
}
