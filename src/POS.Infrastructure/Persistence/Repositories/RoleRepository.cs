using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Rbac;

namespace POS.Infrastructure.Persistence.Repositories;

public class RoleRepository(AppDbContext context) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ThenInclude(p => p.Resource)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<List<Role>> GetSystemRolesByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Roles.Where(r => r.IsSystemRole && r.StoreId == null && r.Name == name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetRolesAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        var query = context.Roles.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(r => r.IsSystemRole || r.StoreId == storeId.Value);
        }

        return await query
            .OrderBy(r => r.IsSystemRole ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? storeId, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        return context.Roles.AnyAsync(r =>
            r.Name.ToLower() == normalizedName &&
            r.StoreId == storeId &&
            (!excludeRoleId.HasValue || r.Id != excludeRoleId.Value),
            cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await context.Roles.AddAsync(role, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetEmployeeIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await context.Employees
            .Where(e => e.RoleId == roleId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdatePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, Guid? actorId = null, CancellationToken cancellationToken = default)
    {
        var existingRolePermissions = await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        context.RolePermissions.RemoveRange(existingRolePermissions);

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role != null)
        {
            var targetIds = permissionIds.Distinct().ToList();
            var newPermissions = await context.Permissions
                .Where(p => targetIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var newRolePermissions = newPermissions.Select(p =>
                new RolePermission(roleId, role, p.Id, p, actorId)).ToList();

            await context.RolePermissions.AddRangeAsync(newRolePermissions, cancellationToken);
        }
    }
}
