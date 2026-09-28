using POS.Domain.Common;
using POS.Domain.Products;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.Stock;

public class StockBatch : BaseEntity
{
    public StockBatch() : base()
    {
    }

    public StockBatch(Guid storeId, Guid skuId, string batchNo, decimal qty, DateOnly? expiryDate = null, Guid? id = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(batchNo))
            throw new ArgumentException("Số lô không được để trống.", nameof(batchNo));
        if (qty < 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Số lượng lô không được âm.");

        StoreId = storeId;
        SkuId = skuId;
        BatchNo = batchNo.Trim();
        Qty = qty;
        ExpiryDate = expiryDate;
        ReceivedAt = DateTime.UtcNow;
    }

    public void DeductQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Số lượng trừ phải lớn hơn 0.");
        if (qty > Qty)
            throw new InvalidOperationException($"Số lượng trừ ({qty}) vượt quá tồn kho hiện tại của lô ({Qty}).");

        Qty -= qty;
    }

    public void AddQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Số lượng cộng phải lớn hơn 0.");

        Qty += qty;
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public string BatchNo { get; private set; } = default!;
    public decimal Qty { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public DateTime ReceivedAt { get; private set; } = DateTime.UtcNow;
}
