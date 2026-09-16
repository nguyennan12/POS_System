using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Infrastructure.Persistence.Configurations;

namespace POS.Infrastructure.Persistence.Seeders;

public class ResourcePermissionSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        foreach (var resDef in SystemPermissions.Resources)
        {
            var resource = await context.Resources
                .Include(item => item.Permissions)
                .SingleOrDefaultAsync(item => item.Code == resDef.Code, cancellationToken);

            if (resource is null)
            {
                resource = new Resource(resDef.Code, resDef.Description);
                await context.Resources.AddAsync(resource, cancellationToken);
            }

            foreach (var (action, description) in resDef.Actions)
            {
                if (resource.Permissions.Any(permission => permission.Action == action))
                    continue;

                resource.Permissions.Add(new Permission(resource.Id, resource, action, description));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}