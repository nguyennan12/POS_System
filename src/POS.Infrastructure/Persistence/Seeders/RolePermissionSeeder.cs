using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Seeders;

public class RolePermissionSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.RolePermissions.AnyAsync(cancellationToken))
            return;

        var roles = await context.Roles.ToListAsync(cancellationToken);
        var permissions = await context.Permissions.Include(p => p.Resource).ToListAsync(cancellationToken);

        var ownerRole = roles.FirstOrDefault(r => r.Name == RoleNames.Owner);
        var adminRole = roles.FirstOrDefault(r => r.Name == RoleNames.Admin);
        var managerRole = roles.FirstOrDefault(r => r.Name == RoleNames.Manager);
        var cashierRole = roles.FirstOrDefault(r => r.Name == RoleNames.Cashier);

        if (ownerRole == null || adminRole == null || managerRole == null || cashierRole == null)
        {
            throw new InvalidOperationException("Default system roles must be seeded before seeding RolePermissions.");
        }

        var rolePermissions = new List<RolePermission>();

        // 1. OWNER: Full Access
        foreach (var p in permissions)
        {
            rolePermissions.Add(new RolePermission(ownerRole.Id, ownerRole, p.Id, p));
        }

        // 2. ADMIN: All except create/delete store
        var adminPerms = permissions.Where(p =>
            !(p.Resource.Code == ResourceNames.Stores && (p.Action == PermissionAction.Create || p.Action == PermissionAction.Delete)));
        foreach (var p in adminPerms)
        {
            rolePermissions.Add(new RolePermission(adminRole.Id, adminRole, p.Id, p));
        }

        // 3. MANAGER: Operations (orders, inventory, customers, categories, reports)
        var managerResources = new[]
        {
            ResourceNames.Orders,
            ResourceNames.Inventory,
            ResourceNames.Customers,
            ResourceNames.Categories,
            ResourceNames.Reports
        };
        var managerPerms = permissions.Where(p => managerResources.Contains(p.Resource.Code));
        foreach (var p in managerPerms)
        {
            rolePermissions.Add(new RolePermission(managerRole.Id, managerRole, p.Id, p));
        }

        // 4. CASHIER: POS cashier operations (read-all + create order/customer)
        var cashierPerms = permissions.Where(p =>
            p.Action == PermissionAction.Read ||
            (p.Resource.Code == ResourceNames.Orders && p.Action == PermissionAction.Create) ||
            (p.Resource.Code == ResourceNames.Customers && p.Action == PermissionAction.Create));
        foreach (var p in cashierPerms)
        {
            rolePermissions.Add(new RolePermission(cashierRole.Id, cashierRole, p.Id, p));
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}