using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Seeders;

public class RolePermissionSeeder : ISeeder
{
    public static readonly HashSet<string> CashierPermissionCodes =
    [
        "stores:read",
        "categories:read",
        "products:read",
        "skus:read",
        "inventory:read",
        "shifts:manage_own",
        "orders:create_own",
        "orders:read_own",
        "orders:cancel_own",
        "payments:create_own",
        "payments:read",
        "invoices:read",
        "customers:read",
        "customers:create",
        "customers:manage",
        "member_tiers:read",
        "promotions:read",
        "vouchers:read",
        "vouchers:apply_own",
        "config:read"
    ];

    public static readonly HashSet<string> OwnerOnlyPermissionCodes =
    [
        "stores:manage",
        "roles:manage",
        "member_tiers:manage",
        "reports:profit_view",
        "config:manage_global",
        "audit_logs:read"
    ];

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

        var ownerExpectedCodes = permissions.Select(p => p.Code).ToHashSet();
        var storeManagerExpectedCodes = permissions.Where(p => !OwnerOnlyPermissionCodes.Contains(p.Code)).Select(p => p.Code).ToHashSet();
        var cashierExpectedCodes = permissions.Where(p => CashierPermissionCodes.Contains(p.Code)).Select(p => p.Code).ToHashSet();

        var existingRolePermissions = await context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        // 1. Remove stale / revoked role permissions for the 3 system roles
        var staleRolePermissions = existingRolePermissions.Where(rp =>
        {
            if (rp.Role.Name == RoleNames.Owner)
                return !ownerExpectedCodes.Contains(rp.Permission.Code);
            if (rp.Role.Name == RoleNames.StoreManager)
                return !storeManagerExpectedCodes.Contains(rp.Permission.Code);
            if (rp.Role.Name == RoleNames.Cashier)
                return !cashierExpectedCodes.Contains(rp.Permission.Code);
            return false;
        }).ToList();

        if (staleRolePermissions.Count > 0)
        {
            context.RolePermissions.RemoveRange(staleRolePermissions);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Refresh active map of (roleId, permissionId)
        var activeMap = (await context.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var newRolePermissions = new List<RolePermission>();

        // 3. Add expected permissions for Owner (Full Access - 57 permissions)
        foreach (var p in permissions)
        {
            AddIfMissing(ownerRole, p);
        }

        // 4. Add expected permissions for StoreManager (Store Management - 51 permissions)
        foreach (var p in permissions.Where(p => storeManagerExpectedCodes.Contains(p.Code)))
        {
            AddIfMissing(storeManagerRole, p);
        }

        // 5. Add expected permissions for Cashier (Front-desk POS Cashier - 20 permissions)
        foreach (var p in permissions.Where(p => cashierExpectedCodes.Contains(p.Code)))
        {
            AddIfMissing(cashierRole, p);
        }

        if (newRolePermissions.Count > 0)
        {
            await context.RolePermissions.AddRangeAsync(newRolePermissions, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        void AddIfMissing(Role role, Permission permission)
        {
            var key = new { RoleId = role.Id, PermissionId = permission.Id };
            if (activeMap.Contains(key))
                return;

            activeMap.Add(key);
            newRolePermissions.Add(new RolePermission(role.Id, role, permission.Id, permission));
        }
    }
}