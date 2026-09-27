using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Shifts.Commands.OpenShift;

namespace POS.Application.UseCases.Shifts.Queries.GetShiftById;

public record GetShiftByIdQuery(Guid ShiftId) : IQuery<ShiftDto>, IRequirePermission
{
    public string RequiredPermission => "shifts:read";
}
