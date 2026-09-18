using Microsoft.EntityFrameworkCore;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;

namespace POS.Infrastructure.Persistence.Seeders;

public class MemberTierSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.MemberTiers.AnyAsync(cancellationToken))
            return;

        var tiers = new[]
        {
            new MemberTier(MemberTierName.Normal, minSpending: 0m, pointRate: 0.01m, discountRate: 0m, displayColor: "#808080"),
            new MemberTier(MemberTierName.Silver, minSpending: 5_000_000m, pointRate: 0.015m, discountRate: 0.02m, displayColor: "#C0C0C0"),
            new MemberTier(MemberTierName.Gold, minSpending: 15_000_000m, pointRate: 0.02m, discountRate: 0.05m, displayColor: "#FFD700"),
            new MemberTier(MemberTierName.VIP, minSpending: 30_000_000m, pointRate: 0.03m, discountRate: 0.10m, displayColor: "#9400D3")
        };

        await context.MemberTiers.AddRangeAsync(tiers, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
