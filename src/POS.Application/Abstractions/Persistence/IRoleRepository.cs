using POS.Domain.Rbac;

namespace POS.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Role>> GetSystemRolesByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? storeId, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetEmployeeIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task UpdatePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, Guid? actorId = null, CancellationToken cancellationToken = default);
}
