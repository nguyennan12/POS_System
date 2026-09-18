using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Seeders;

public class RolePermissionSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var roles = await context.Roles.ToListAsync(cancellationToken);
        var permissions = await context.Permissions.Include(p => p.Resource).ToListAsync(cancellationToken);

        var ownerRole = roles.FirstOrDefault(r => r.Name == RoleNames.Owner);
        var storeManagerRole = roles.FirstOrDefault(r => r.Name == RoleNames.StoreManager);
        var cashierRole = roles.FirstOrDefault(r => r.Name == RoleNames.Cashier);

        if (ownerRole == null || storeManagerRole == null || cashierRole == null)
        {
            throw new InvalidOperationException("Default system roles must be seeded before seeding RolePermissions.");
        }

        var existingRolePermissions = await context.RolePermissions
            .Select(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
            .ToHashSetAsync(cancellationToken);
        var rolePermissions = new List<RolePermission>();

        // 1. OWNER: Full Access
        foreach (var p in permissions)
        {
            AddIfMissing(ownerRole, p);
        }

        // 2. STORE_MANAGER: All permissions except create/delete store
        var storeManagerPerms = permissions.Where(p =>
            !(p.Resource.Code == ResourceNames.Stores && (p.Action == PermissionAction.Create || p.Action == PermissionAction.Delete)));
        foreach (var p in storeManagerPerms)
        {
            AddIfMissing(storeManagerRole, p);
        }

        // 3. CASHIER: POS cashier operations (read-all + create order/customer + update customer)
        var cashierPerms = permissions.Where(p =>
            p.Action == PermissionAction.Read ||
            (p.Resource.Code == ResourceNames.Orders && p.Action == PermissionAction.Create) ||
            (p.Resource.Code == ResourceNames.Customers && (p.Action == PermissionAction.Create || p.Action == PermissionAction.Update)));
        foreach (var p in cashierPerms)
        {
            AddIfMissing(cashierRole, p);
        }

        if (rolePermissions.Count > 0)
        {
            await context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        void AddIfMissing(Role role, Permission permission)
        {
            if (existingRolePermissions.Contains(new { RoleId = role.Id, PermissionId = permission.Id }))
                return;

            existingRolePermissions.Add(new { RoleId = role.Id, PermissionId = permission.Id });
            rolePermissions.Add(new RolePermission(role.Id, role, permission.Id, permission));
        }
    }
}