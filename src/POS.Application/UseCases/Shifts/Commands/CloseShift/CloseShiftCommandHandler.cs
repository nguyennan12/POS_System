using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Shifts.Commands.CloseShift;

public class CloseShiftCommandHandler(
    IShiftRepository shiftRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseShiftCommand, ShiftSummaryDto>
{
    public async Task<Result<ShiftSummaryDto>> Handle(
        CloseShiftCommand command,
        CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift is null)
            return ShiftErrors.NotFound;

        // Aggregate sales data from orders/payments in this shift
        var sales = await shiftRepository.GetShiftSalesAsync(shift.Id, cancellationToken);

        // ExpectedCash = what the register should contain
        var expectedCash = shift.OpeningCash + sales.CashSales - sales.Refunds;

        var closeResult = shift.Close(command.ActualCash, expectedCash, command.Note);
        if (closeResult.IsFailure)
            return closeResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var difference = command.ActualCash - expectedCash;

        return new ShiftSummaryDto(
            shift.Id,
            shift.OpeningCash,
            sales.CashSales,
            sales.CardSales,
            sales.QrSales,
            sales.Refunds,
            expectedCash,
            command.ActualCash,
            difference,
            sales.OrderCount,
            new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
            shift.ClosedAt.HasValue
                ? new DateTimeOffset(shift.ClosedAt.Value, TimeSpan.Zero)
                : null);
    }
}
