using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Rbac;

namespace POS.Infrastructure.Persistence.Repositories;

public class PermissionRepository(AppDbContext context) : IPermissionRepository
{
    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Permissions
            .Include(p => p.Resource)
            .AsQueryable();

        if (resourceId.HasValue)
        {
            query = query.Where(p => p.ResourceId == resourceId.Value);
        }

        return await query
            .OrderBy(p => p.Resource.Code)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetResourcesWithPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Resources
            .Include(r => r.Permissions)
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var targetIds = permissionIds.Distinct().ToList();
        return await context.Permissions
            .Include(p => p.Resource)
            .Where(p => targetIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .ThenInclude(p => p.Resource)
            .Select(rp => rp.Permission)
            .OrderBy(p => p.Resource.Code)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }
}