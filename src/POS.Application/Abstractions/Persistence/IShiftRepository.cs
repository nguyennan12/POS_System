using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IShiftRepository
{
    Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shift?> GetOpenShiftAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task AddAsync(Shift shift, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregate paid-order payments for a shift, grouped by payment method.
    /// Returns (CashSales, CardSales, QrSales, Refunds, OrderCount).
    /// </summary>
    Task<ShiftSalesData> GetShiftSalesAsync(Guid shiftId, CancellationToken cancellationToken = default);
}

public record ShiftSalesData(
    decimal CashSales,
    decimal CardSales,
    decimal QrSales,
    decimal Refunds,
    int OrderCount);
