using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Shifts.Commands.OpenShift;

public record OpenShiftCommand(
    Guid StoreId,
    Guid RegisterId,
    decimal OpeningCash,
    string? Note
) : ICommand<ShiftDto>, IRequirePermission
{
    public string RequiredPermission => "shifts:manage_own";
}

public record ShiftDto(
    Guid Id,
    Guid StoreId,
    Guid RegisterId,
    string RegisterName,
    Guid EmployeeId,
    string EmployeeName,
    decimal OpeningCash,
    decimal? ClosingCash,
    decimal? ActualCash,
    string Status,
    string? Note,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);
