using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Shifts.Commands.OpenShift;

public record OpenShiftCommand(
    Guid StoreId,
    decimal OpeningCash,
    string? Note
) : ICommand<ShiftDto>;

public record ShiftDto(
    Guid Id,
    Guid StoreId,
    Guid EmployeeId,
    string EmployeeName,
    decimal OpeningCash,
    decimal? ClosingCash,
    decimal? ActualCash,
    string Status,
    string? Note,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);
