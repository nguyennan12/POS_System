using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;
using POS.Domain.Rbac.Errors;

namespace POS.Application.UseCases.Rbac.Commands.UpdateRolePermissions;

public class UpdateRolePermissionsCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IEmployeeStoreAccessRepository accessRepository,
    ICacheService cacheService,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateRolePermissionsCommand, RoleDetailDto>
{
    public async Task<Result<RoleDetailDto>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return RoleErrors.NotFound;
        }

        if (role.IsSystemRole)
        {
            return RoleErrors.SystemRoleCannotBeModified;
        }

        var canManage = await RoleAccessControl.CanManageStoreAsync(
            currentUser, role.StoreId, accessRepository, cancellationToken);
        if (!canManage)
        {
            return RoleErrors.Forbidden;
        }

        var distinctPermissionIds = request.PermissionIds.Distinct().ToList();
        var permissions = await permissionRepository.GetByIdsAsync(distinctPermissionIds, cancellationToken);

        if (permissions.Count != distinctPermissionIds.Count)
        {
            return RoleErrors.InvalidPermissionIds;
        }

        await roleRepository.UpdatePermissionsAsync(
            request.RoleId,
            distinctPermissionIds,
            currentUser.EmployeeId,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Batch Invalidation of Redis Cache for all affected employees
        var employeeIds = await roleRepository.GetEmployeeIdsByRoleIdAsync(role.Id, cancellationToken);
        if (employeeIds.Count > 0)
        {
            var cacheKeys = employeeIds.Select(id => $"perm:{id}");
            await cacheService.RemoveRangeAsync(cacheKeys, cancellationToken);
        }

        var permissionDtos = permissions.Select(p => new PermissionDto(
            p.Id,
            p.ResourceId,
            p.Resource.Code,
            p.Action.ToString(),
            p.Code,
            p.Description)).ToList();

        return new RoleDetailDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.StoreId,
            permissionDtos,
            role.CreatedAt,
            DateTime.UtcNow);
    }
}
