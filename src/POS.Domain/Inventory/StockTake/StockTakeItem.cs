using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Inventory.StockTake;

public class StockTakeItem : BaseEntity
{
    public StockTakeItem() : base()
    {
    }

    public Guid TakeId { get; private set; }
    public StockTake Take { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public decimal SystemQty { get; private set; }
    public decimal ActualQty { get; private set; }
    public decimal DiffQty { get; private set; }
    public string? Note { get; private set; }
}
