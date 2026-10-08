using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Enums;
using POS.Infrastructure.Persistence.Configurations;

namespace POS.Infrastructure.Persistence.Seeders;

public class ResourcePermissionSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        // 1. Build expected sets from SystemPermissions
        var expectedResourceCodes = SystemPermissions.Resources.Select(r => r.Code.ToLowerInvariant()).ToHashSet();
        var expectedPermissionCodes = SystemPermissions.Resources
            .SelectMany(r => r.Actions.Select(a => $"{r.Code.ToLowerInvariant()}:{Permission.ActionToCode(a.Action)}"))
            .ToHashSet();

        // 2. Fetch existing from DB
        var existingPermissions = await context.Permissions.Include(p => p.Resource).ToListAsync(cancellationToken);
        var existingResources = await context.Resources.Include(r => r.Permissions).ToListAsync(cancellationToken);

        // 3. Delete stale permissions that are no longer in SystemPermissions
        var stalePermissions = existingPermissions
            .Where(p => !expectedPermissionCodes.Contains(p.Code.ToLowerInvariant()))
            .ToList();

        if (stalePermissions.Count > 0)
        {
            context.Permissions.RemoveRange(stalePermissions);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 4. Delete stale resources
        var staleResources = existingResources
            .Where(r => !expectedResourceCodes.Contains(r.Code.ToLowerInvariant()))
            .ToList();

        if (staleResources.Count > 0)
        {
            context.Resources.RemoveRange(staleResources);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 5. Ensure all defined resources and permissions exist
        foreach (var resDef in SystemPermissions.Resources)
        {
            var resource = await context.Resources
                .Include(item => item.Permissions)
                .SingleOrDefaultAsync(item => item.Code == resDef.Code, cancellationToken);

            if (resource is null)
            {
                resource = new Resource(resDef.Code, resDef.Description);
                await context.Resources.AddAsync(resource, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            foreach (var (action, description) in resDef.Actions)
            {
                var existingPerm = resource.Permissions.FirstOrDefault(p => p.Action == action);
                if (existingPerm is null)
                {
                    resource.Permissions.Add(new Permission(resource.Id, resource, action, description));
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}