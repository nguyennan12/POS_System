using POS.Domain.Common;
using POS.Domain.Employees.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Employees;

public class Shift : BaseEntity
{
    public Shift() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = default!;

    public decimal OpeningCash { get; private set; }
    public decimal? ClosingCash { get; private set; }
    public decimal? ActualCash { get; private set; }
    public ShiftStatus Status { get; private set; } = ShiftStatus.Open;
    public string? Note { get; private set; }
    public DateTime OpenedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }
}
