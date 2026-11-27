using Microsoft.EntityFrameworkCore;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Orders;
using POS.Domain.Products;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;

namespace POS.Infrastructure.Tests.Customers;

public sealed class CustomerRepositoryStoreScopeTests
{
    [Fact]
    public async Task StoreScopeFiltersCustomerSearchBeforeCountingAndPaging()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new CustomerTestDbContext(options);
        var storeId = Guid.NewGuid();
        var otherStoreId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var tier = new MemberTier(MemberTierName.Normal, 0m, 0.01m, 0m, null);
        var local = new Customer("Local", "0900000001", tier.Id);
        var other = new Customer("Other branch", "0900000002", tier.Id);
        var unlinked = new Customer("Unlinked", "0900000003", tier.Id);
        context.MemberTiers.Add(tier);
        context.Customers.AddRange(local, other, unlinked);
        context.Orders.AddRange(
            Order.CreateDraft(storeId, shiftId, employeeId, local.Id),
            Order.CreateDraft(storeId, shiftId, employeeId, local.Id),
            Order.CreateDraft(otherStoreId, shiftId, employeeId, other.Id));
        await context.SaveChangesAsync();
        var repository = new CustomerRepository(context);

        var (items, count) = await repository.GetPagedAsync("090", null, null, null, 1, 1, storeId: storeId);

        Assert.Equal(1, count);
        Assert.Equal(local.Id, Assert.Single(items).Customer.Id);
        Assert.True(await repository.HasOrdersAtStoreAsync(local.Id, storeId));
        Assert.False(await repository.HasOrdersAtStoreAsync(other.Id, storeId));
        Assert.False(await repository.HasOrdersAtStoreAsync(unlinked.Id, storeId));
    }

    private sealed class CustomerTestDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // This PostgreSQL jsonb property is unused by the CRM test.
            modelBuilder.Entity<Sku>().Ignore(sku => sku.Attributes);
        }
    }
}
