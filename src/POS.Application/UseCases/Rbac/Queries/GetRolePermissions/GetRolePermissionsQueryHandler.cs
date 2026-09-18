using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;
using POS.Domain.Rbac.Errors;

namespace POS.Application.UseCases.Rbac.Queries.GetRolePermissions;

public class GetRolePermissionsQueryHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IEmployeeStoreAccessRepository accessRepository,
    ICurrentUser currentUser
) : IQueryHandler<GetRolePermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return RoleErrors.NotFound;
        }

        var canRead = await RoleAccessControl.CanReadRoleAsync(currentUser, role, accessRepository, cancellationToken);
        if (!canRead)
        {
            return RoleErrors.Forbidden;
        }

        var permissions = await permissionRepository.GetPermissionsByRoleIdAsync(request.RoleId, cancellationToken);

        var dtos = permissions.Select(p => new PermissionDto(
            p.Id,
            p.ResourceId,
            p.Resource.Code,
            p.Action.ToString(),
            p.Code,
            p.Description
        )).ToList();

        return dtos;
    }
}
