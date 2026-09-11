using Microsoft.EntityFrameworkCore;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;

namespace POS.Infrastructure.Persistence.Seeders;

public class RoleSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Roles.AnyAsync(cancellationToken))
            return;

        var roles = new[]
        {
            new Role(RoleNames.Owner, isSystemRole: true),
            new Role(RoleNames.Admin, isSystemRole: true),
            new Role(RoleNames.Manager, isSystemRole: true),
            new Role(RoleNames.Cashier, isSystemRole: true)
        };

        await context.Roles.AddRangeAsync(roles, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}