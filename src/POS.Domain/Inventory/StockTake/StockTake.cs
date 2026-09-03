using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.StockTake;

public class StockTake : BaseEntity
{
    public StockTake() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public StockTakeStatus Status { get; private set; } = StockTakeStatus.Draft;
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public Guid? ApprovedBy { get; private set; }
    public Employee? ApprovedByEmployee { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; private set; }

    public ICollection<StockTakeItem> Items { get; private set; } = new List<StockTakeItem>();
}
