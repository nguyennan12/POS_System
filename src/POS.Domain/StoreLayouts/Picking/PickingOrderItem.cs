using POS.Domain.Common;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Domain.StoreLayouts.Picking;

public class PickingOrderItem : BaseEntity
{
    public Guid PickingOrderId { get; private set; }
    public Guid SkuId { get; private set; }
    public decimal RequestedQty { get; private set; }
    public decimal PickedQty { get; private set; }
    public PickingOrderItemStatus Status { get; private set; }
    public Guid? PlacementId { get; private set; }
    public string ShelfCodeSnapshot { get; private set; } = string.Empty;
    public int LevelNumberSnapshot { get; private set; }
    public string ZoneNameSnapshot { get; private set; } = string.Empty;
    public DateTime? ConfirmedAt { get; private set; }
    public string? Note { get; private set; }

    public PickingOrder? PickingOrder { get; private set; }

    private PickingOrderItem() { } // EF Core

    public PickingOrderItem(
        Guid pickingOrderId,
        Guid skuId,
        decimal requestedQty,
        Guid? placementId,
        string shelfCodeSnapshot,
        int levelNumberSnapshot,
        string zoneNameSnapshot,
        string? note = null,
        Guid? id = null) : base(id)
    {
        PickingOrderId = pickingOrderId;
        SkuId = skuId;
        RequestedQty = requestedQty;
        PickedQty = 0;
        Status = PickingOrderItemStatus.Pending;
        PlacementId = placementId;
        ShelfCodeSnapshot = shelfCodeSnapshot;
        LevelNumberSnapshot = levelNumberSnapshot;
        ZoneNameSnapshot = zoneNameSnapshot;
        Note = note;
    }

    public void ConfirmPicked(decimal qty, DateTime now)
    {
        PickedQty = qty;
        Status = qty >= RequestedQty ? PickingOrderItemStatus.Picked : PickingOrderItemStatus.Substituted;
        ConfirmedAt = now;
    }

    public void MarkMissing(DateTime now, string? note = null)
    {
        Status = PickingOrderItemStatus.Missing;
        ConfirmedAt = now;
        if (note != null) Note = note;
    }
}
