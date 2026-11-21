using POS.Domain.Common;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Domain.StoreLayouts;

public class ProductPlacementHistory : BaseEntity
{
    public Guid LayoutId { get; private set; }
    public Guid SkuId { get; private set; }
    public PlacementHistoryAction Action { get; private set; }
    public Guid? FromShelfLevelId { get; private set; }
    public Guid? ToShelfLevelId { get; private set; }
    public Guid PerformedBy { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductPlacementHistory() { } // EF Core

    public ProductPlacementHistory(
        Guid layoutId,
        Guid skuId,
        PlacementHistoryAction action,
        Guid? fromShelfLevelId,
        Guid? toShelfLevelId,
        Guid performedBy,
        string? note,
        DateTime now,
        Guid? id = null) : base(id)
    {
        LayoutId = layoutId;
        SkuId = skuId;
        Action = action;
        FromShelfLevelId = fromShelfLevelId;
        ToShelfLevelId = toShelfLevelId;
        PerformedBy = performedBy;
        Note = note;
        CreatedAt = now;
    }
}
