using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Queries.GetPermissions;

public record GetPermissionsQuery(Guid? ResourceId = null) : IQuery<IReadOnlyList<PermissionDto>>, IRequirePermission
{
  public string RequiredPermission => "roles:read";
}
