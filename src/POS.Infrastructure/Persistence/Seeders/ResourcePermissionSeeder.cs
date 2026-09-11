using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Infrastructure.Persistence.Configurations;

namespace POS.Infrastructure.Persistence.Seeders;

public class ResourcePermissionSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Resources.AnyAsync(cancellationToken))
            return;

        foreach (var resDef in SystemPermissions.Resources)
        {
            var resource = new Resource(resDef.Code, resDef.Description);
            foreach (var (act, actDesc) in resDef.Actions)
            {
                var permission = new Permission(resource.Id, resource, act, actDesc);
                resource.Permissions.Add(permission);
            }

            await context.Resources.AddAsync(resource, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}