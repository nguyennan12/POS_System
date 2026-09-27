using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Commands.UpdateRole;

public record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Description = null
) : ICommand<RoleDto>, IRequirePermission
{
    public string RequiredPermission => "roles:update";
}
