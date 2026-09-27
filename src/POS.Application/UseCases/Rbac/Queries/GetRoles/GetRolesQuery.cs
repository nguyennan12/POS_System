using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Queries.GetRoles;

public record GetRolesQuery(Guid? StoreId = null) : IQuery<IReadOnlyList<RoleDto>>, IRequirePermission
{
    public string RequiredPermission => "roles:read";
}
