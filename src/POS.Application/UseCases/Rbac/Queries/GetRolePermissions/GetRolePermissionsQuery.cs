using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Queries.GetRolePermissions;

public record GetRolePermissionsQuery(Guid RoleId) : IQuery<IReadOnlyList<PermissionDto>>, IRequirePermission
{
  public string RequiredPermission => "roles:read";
}
