using POS.Domain.Common;
using POS.Domain.Products;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.Stock;

public class StockEntry : BaseEntity
{
    public StockEntry() : base()
    {
    }

    public StockEntry(Guid storeId, Guid skuId, decimal qtyOnHand, decimal minStock = 0, Guid? id = null) : base(id)
    {
        StoreId = storeId;
        SkuId = skuId;
        QtyOnHand = qtyOnHand;
        MinStock = minStock;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Tăng tồn kho và tính lại giá vốn bình quân (Weighted Average Cost).
    /// Giá vốn bình quân = (Tồn kho hiện tại × Giá vốn cũ + Số lượng nhập × Đơn giá nhập) / Tổng số lượng sau nhập.
    /// </summary>
    public void IncreaseStock(decimal qty, decimal unitCost)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));

        var totalValue = QtyOnHand * AverageCost + qty * unitCost;
        QtyOnHand += qty;
        AverageCost = QtyOnHand > 0 ? totalValue / QtyOnHand : unitCost;
        LastUpdated = DateTime.UtcNow;
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public decimal QtyOnHand { get; private set; }
    public decimal MinStock { get; private set; }
    /// <summary>Giá vốn bình quân gia quyền (Weighted Average Cost).</summary>
    public decimal AverageCost { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
}

