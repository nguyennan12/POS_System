using POS.Domain.Common;
using POS.Domain.Employees.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Employees;

public class Shift : BaseEntity
{
    public Shift() : base()
    {
    }

    private Shift(
        Guid storeId,
        Guid employeeId,
        decimal openingCash,
        string? note,
        Guid? id = null)
        : base(id)
    {
        StoreId = storeId;
        EmployeeId = employeeId;
        OpeningCash = openingCash;
        Note = note;
        Status = ShiftStatus.Open;
        OpenedAt = DateTime.UtcNow;
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

    /// <summary>
    /// Factory method to open a new shift.
    /// </summary>
    public static Shift Open(Guid storeId, Guid employeeId, decimal openingCash, string? note)
    {
        return new Shift(storeId, employeeId, openingCash, note);
    }

    /// <summary>
    /// Close the shift with actual cash count and the system-calculated expected cash.
    /// ClosingCash = expectedCash (system-calculated: OpeningCash + CashSales - CashRefunds).
    /// Difference = ActualCash - ClosingCash.
    /// </summary>
    public Result Close(decimal actualCash, decimal expectedCash, string? note)
    {
        if (Status == ShiftStatus.Closed)
            return ShiftErrors.AlreadyClosed;

        ActualCash = actualCash;
        ClosingCash = expectedCash;
        Status = ShiftStatus.Closed;
        ClosedAt = DateTime.UtcNow;

        if (note is not null)
            Note = note;

        return Result.Success();
    }
}
