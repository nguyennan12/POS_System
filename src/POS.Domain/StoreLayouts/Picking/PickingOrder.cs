using POS.Domain.Common;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Domain.StoreLayouts.Picking;

public class PickingOrder : BaseEntity
{
    public Guid StoreId { get; private set; }
    public Guid LayoutId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public PickingOrderStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<PickingOrderItem> _items = [];
    public IReadOnlyCollection<PickingOrderItem> Items => _items.AsReadOnly();

    private PickingOrder() { } // EF Core

    public PickingOrder(
        Guid storeId,
        Guid layoutId,
        Guid createdBy,
        DateTime now,
        Guid? orderId = null,
        Guid? assignedTo = null,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        LayoutId = layoutId;
        CreatedBy = createdBy;
        CreatedAt = now;
        OrderId = orderId;
        AssignedTo = assignedTo;
        Status = PickingOrderStatus.Pending;
    }

    public void AddItem(PickingOrderItem item) => _items.Add(item);

    public void AssignTo(Guid employeeId)
    {
        AssignedTo = employeeId;
    }

    public void StartPicking(DateTime now)
    {
        if (Status == PickingOrderStatus.Pending)
        {
            Status = PickingOrderStatus.Picking;
            StartedAt ??= now;
        }
    }

    public void Complete(DateTime now)
    {
        Status = _items.All(i => i.Status == PickingOrderItemStatus.Picked)
            ? PickingOrderStatus.Completed
            : PickingOrderStatus.Partial;
        CompletedAt = now;
    }

    public void Cancel()
    {
        Status = PickingOrderStatus.Cancelled;
    }
}
