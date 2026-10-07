using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Products;

public class Sku : BaseEntity
{
    public Sku() : base()
    {
    }

    public Sku(
        Guid productId,
        Guid storeId,
        string skuCode,
        string barcode,
        decimal sellPrice,
        decimal costPrice = 0,
        decimal taxRate = 0,
        bool isActive = true,
        Dictionary<string, string>? attributes = null,
        Guid? id = null) : base(id)
    {
        ProductId = productId;
        StoreId = storeId;
        SkuCode = skuCode;
        Barcode = barcode;
        SellPrice = sellPrice;
        CostPrice = costPrice;
        TaxRate = taxRate;
        IsActive = isActive;
        Attributes = attributes;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; set; } = default!;

    public Guid StoreId { get; private set; }
    public Store Store { get; set; } = default!;

    public string SkuCode { get; private set; } = default!;
    public string Barcode { get; private set; } = default!;
    public Dictionary<string, string>? Attributes { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SellPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Danh sách đơn vị tính quy đổi (lazy-load / eager-load tuỳ query).</summary>
    public IReadOnlyList<UnitConversion>? UnitConversions { get; private set; }

    public void Update(
        string skuCode,
        string barcode,
        decimal sellPrice,
        decimal costPrice,
        decimal taxRate,
        bool isActive,
        Dictionary<string, string>? attributes)
    {
        SkuCode = skuCode;
        Barcode = barcode;
        SellPrice = sellPrice;
        CostPrice = costPrice;
        TaxRate = taxRate;
        IsActive = isActive;
        Attributes = attributes;
        UpdatedAt = DateTime.UtcNow;
    }
}
