using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Inventory.StockTake;

public class StockTakeItem : BaseEntity
{
    public StockTakeItem() : base()
    {
    }

    public StockTakeItem(
        Guid takeId,
        Guid skuId,
        decimal systemQty,
        decimal actualQty,
        string? note = null,
        Guid? id = null) : base(id)
    {
        TakeId = takeId;
        SkuId = skuId;
        SystemQty = systemQty;
        ActualQty = actualQty;
        DiffQty = actualQty - systemQty;
        Note = note;
    }

    public void Update(decimal actualQty, decimal systemQty, string? note = null)
    {
        ActualQty = actualQty;
        SystemQty = systemQty;
        DiffQty = actualQty - systemQty;
        if (note is not null)
        {
            Note = note;
        }
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
