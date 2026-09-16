using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Shifts.Commands.CloseShift;

public record CloseShiftCommand(
    Guid ShiftId,
    decimal ActualCash,
    string? Note
) : ICommand<ShiftSummaryDto>;

public record ShiftSummaryDto(
    Guid ShiftId,
    decimal OpeningCash,
    decimal TotalCashSales,
    decimal TotalCardSales,
    decimal TotalQrSales,
    decimal TotalRefunds,
    decimal ExpectedCash,
    decimal? ActualCash,
    decimal? Difference,
    int OrderCount,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);
