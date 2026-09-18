using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Commands.UpdateRolePermissions;

public record UpdateRolePermissionsCommand(
    Guid RoleId,
    IReadOnlyList<Guid> PermissionIds
) : ICommand<RoleDetailDto>, IRequirePermission
{
  public string RequiredPermission => "roles:update";
}
