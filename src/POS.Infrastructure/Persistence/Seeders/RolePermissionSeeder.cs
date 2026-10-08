using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Seeders;

public class RolePermissionSeeder : ISeeder
{
    private static readonly HashSet<string> CashierPermissionCodes =
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

    private static readonly HashSet<string> OwnerOnlyPermissionCodes =
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

        var existingRolePermissions = await context.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToHashSetAsync(cancellationToken);

        var rolePermissions = new List<RolePermission>();

        // 1. OWNER: Full Access to all permissions
        foreach (var p in permissions)
        {
            AddIfMissing(ownerRole, p);
        }

        // 2. STORE_MANAGER: All permissions except chain-owner exclusive ones
        var storeManagerPerms = permissions.Where(p => !OwnerOnlyPermissionCodes.Contains(p.Code));
        foreach (var p in storeManagerPerms)
        {
            AddIfMissing(storeManagerRole, p);
        }

        // 3. CASHIER: Front-desk POS cashier permissions
        var cashierPerms = permissions.Where(p => CashierPermissionCodes.Contains(p.Code));
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