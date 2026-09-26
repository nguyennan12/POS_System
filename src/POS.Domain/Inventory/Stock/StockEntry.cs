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

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public decimal QtyOnHand { get; private set; }
    public decimal MinStock { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
}
