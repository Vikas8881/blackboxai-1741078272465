namespace StalkerModels.Models;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountedUnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    
    // For reseller tracking
    public string? ResellerId { get; set; }
    public virtual User? Reseller { get; set; }
    public decimal? ResellerCommission { get; set; }
    public decimal? ResellerCommissionRate { get; set; }
}
