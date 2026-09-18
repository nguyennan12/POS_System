using POS.Domain.Rbac;

namespace POS.Application.Abstractions.Persistence;

public interface IPermissionRepository
{
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> GetAllAsync(
        Guid? resourceId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Resource>> GetResourcesWithPermissionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> GetByIdsAsync(
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> GetPermissionsByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}