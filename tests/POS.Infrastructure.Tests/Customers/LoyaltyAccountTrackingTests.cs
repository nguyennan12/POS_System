using Microsoft.EntityFrameworkCore;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;

namespace POS.Infrastructure.Tests.Customers;

public class LoyaltyAccountTrackingTests
{
    [Fact]
    public async Task AddLoyaltyAccount_ShouldPreserveTrackedCustomerAndTier()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=tracking_only").Options;
        await using var context = new AppDbContext(options);
        var tier = new MemberTier(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");
        var customer = new Customer("Customer", "0901234567", tier.Id);
        // Attach the existing entities as a tracking query would. This verifies
        // PostgreSQL model tracking without opening a database connection.
        context.MemberTiers.Attach(tier);
        context.Customers.Attach(customer);
        var repository = new CustomerRepository(context);
        var account = new LoyaltyAccount(customer.Id, 25);
        await repository.AddLoyaltyAccountAsync(account);

        Assert.Equal(EntityState.Unchanged, context.Entry(customer).State);
        Assert.Equal(EntityState.Unchanged, context.Entry(tier).State);
        Assert.Equal(EntityState.Added, context.Entry(account).State);
        Assert.Same(customer, account.Customer);
        Assert.Same(tier, customer.MemberTier);
    }
}
