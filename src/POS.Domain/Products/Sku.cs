using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Products;

public class Sku : BaseEntity
{
    public Sku() : base()
    {
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public string SkuCode { get; private set; } = default!;
    public string Barcode { get; private set; } = default!;
    public string? AttributesJson { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SellPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
