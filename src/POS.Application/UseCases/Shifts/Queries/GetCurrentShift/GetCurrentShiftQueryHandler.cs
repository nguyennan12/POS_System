using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Shifts.Commands.CloseShift;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Queries.GetCurrentShift;

public class GetCurrentShiftQueryHandler(IShiftRepository shiftRepository)
    : IQueryHandler<GetCurrentShiftQuery, ShiftSummaryDto>
{
    public async Task<Result<ShiftSummaryDto>> Handle(
        GetCurrentShiftQuery query,
        CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetOpenShiftAsync(query.StoreId, cancellationToken);
        if (shift is null)
            return ShiftErrors.NotFound;

        var sales = await shiftRepository.GetShiftSalesAsync(shift.Id, cancellationToken);
        var expectedCash = shift.OpeningCash + sales.CashSales - sales.Refunds;

        return new ShiftSummaryDto(
            shift.Id,
            shift.OpeningCash,
            sales.CashSales,
            sales.CardSales,
            sales.QrSales,
            sales.Refunds,
            expectedCash,
            shift.ActualCash,
            shift.ActualCash.HasValue ? shift.ActualCash.Value - expectedCash : null,
            sales.OrderCount,
            new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
            shift.ClosedAt.HasValue
                ? new DateTimeOffset(shift.ClosedAt.Value, TimeSpan.Zero)
                : null);
    }
}
