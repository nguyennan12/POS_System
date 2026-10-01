using Microsoft.EntityFrameworkCore;
using Npgsql;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Products;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Stores;

namespace POS.Infrastructure.Tests.Invoices;

public class InvoiceReadPersistenceTests
{
    [PostgresFact]
    public async Task GetByIdWithDetails_LoadsOrderItemsSkuProduct_WithoutTracking()
    {
        await using var fixture = await InvoiceDatabase.CreateAsync();
        await using var db = fixture.Db();
        var repository = new InvoiceRepository(db);
        var expected = fixture.Invoices[0];
        var invoice = await repository.GetByIdWithDetailsAsync(expected.Id);
        Assert.NotNull(invoice);
        Assert.Equal(expected.InvoiceNo, invoice.InvoiceNo);
        Assert.Equal(expected.OrderId, invoice.Order.Id);
        Assert.Equal(fixture.PrimaryStoreId, invoice.Order.StoreId);
        var item = Assert.Single(invoice.Order.Items);
        Assert.Equal("SKU-0", item.Sku.SkuCode);
        Assert.Equal("Product-0", item.Sku.Product.Name);
        Assert.Equal(2, item.Qty);
        Assert.Equal(198, item.LineTotal);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Null(await repository.GetByIdWithDetailsAsync(Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task GetPaged_ReturnsDefaultAndSecondPage_WithStableOrderingAndNoTracking()
    {
        await using var fixture = await InvoiceDatabase.CreateAsync();
        await using var db = fixture.Db();
        var repository = new InvoiceRepository(db);
        var first = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, null, null, 1, 20);
        var second = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, null, null, 2, 20);
        Assert.Equal(25, first.TotalCount);
        Assert.Equal(25, second.TotalCount);
        Assert.Equal(20, first.Items.Count);
        Assert.Equal(5, second.Items.Count);
        var expectedIds = fixture.Invoices.OrderByDescending(i => i.IssuedAt).ThenByDescending(i => i.Id)
            .Select(i => i.Id).ToArray();
        Assert.Equal(expectedIds, first.Items.Concat(second.Items).Select(i => i.Id));
        var repeated = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, null, null, 2, 20);
        Assert.Equal(second.Items.Select(i => i.Id), repeated.Items.Select(i => i.Id));
        var maxPage = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, null, null, 1, 100);
        Assert.Equal(25, maxPage.Items.Count);
        var beyond = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, null, null, int.MaxValue, 100);
        Assert.Empty(beyond.Items);
        Assert.Equal(25, beyond.TotalCount);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [PostgresFact]
    public async Task GetPaged_FiltersOrderAndInclusiveUtcBoundaries_AndReturnsEmptyMatches()
    {
        await using var fixture = await InvoiceDatabase.CreateAsync();
        await using var db = fixture.Db();
        var repository = new InvoiceRepository(db);
        var expected = fixture.Invoices[2];
        var instant = new DateTimeOffset(expected.IssuedAt, TimeSpan.Zero);
        var result = await repository.GetPagedAsync(fixture.EmployeeId, null, true, expected.OrderId,
            instant.ToOffset(TimeSpan.FromHours(7)), instant.ToOffset(TimeSpan.FromHours(-5)), 1, 20);
        var actual = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.InvoiceNo, actual.InvoiceNo);
        Assert.Equal(expected.BuyerName, actual.BuyerName);
        Assert.Equal(expected.TotalBeforeTax, actual.TotalBeforeTax);
        Assert.Equal(expected.TaxAmount, actual.TaxAmount);
        Assert.Equal(expected.GrandTotal, actual.GrandTotal);
        Assert.Equal(instant, actual.IssuedAt);

        var range = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null,
            new DateTimeOffset(fixture.Invoices[0].IssuedAt), instant, 1, 20);
        Assert.Equal(3, range.TotalCount); // first two share a timestamp; both boundaries are included
        Assert.Equal(3, range.Items.Count);
        var fromOnly = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, instant, null, 1, 100);
        Assert.Equal(23, fromOnly.TotalCount);
        var toOnly = await repository.GetPagedAsync(fixture.EmployeeId, null, true, null, null, instant, 1, 100);
        Assert.Equal(3, toOnly.TotalCount);
        var empty = await repository.GetPagedAsync(fixture.EmployeeId, null, true, expected.OrderId,
            instant.AddSeconds(1), null, 1, 20);
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.TotalCount);
        var missingOrder = await repository.GetPagedAsync(fixture.EmployeeId, null, true, Guid.NewGuid(), null, null, 1, 20);
        Assert.Empty(missingOrder.Items);
        Assert.Equal(0, missingOrder.TotalCount);
    }

    [PostgresFact]
    public async Task GetPaged_AppliesPrimaryAndGrantedStoreScope_BeforeCountingAndPaging()
    {
        await using var fixture = await InvoiceDatabase.CreateAsync();
        await using var db = fixture.Db();
        var repository = new InvoiceRepository(db);
        var scoped = await repository.GetPagedAsync(fixture.EmployeeId, fixture.PrimaryStoreId, false,
            null, null, null, 1, 100);
        Assert.Equal(24, scoped.TotalCount);
        Assert.Equal(24, scoped.Items.Count);
        Assert.Contains(scoped.Items, i => i.Id == fixture.Invoices[23].Id); // explicitly granted store
        Assert.DoesNotContain(scoped.Items, i => i.Id == fixture.Invoices[24].Id);
        var second = await repository.GetPagedAsync(fixture.EmployeeId, fixture.PrimaryStoreId, false,
            null, null, null, 2, 20);
        Assert.Equal(24, second.TotalCount);
        Assert.Equal(4, second.Items.Count);
        var inaccessible = await repository.GetPagedAsync(fixture.EmployeeId, fixture.PrimaryStoreId, false,
            fixture.Invoices[24].OrderId, null, null, 1, 20);
        Assert.Equal(0, inaccessible.TotalCount);
        Assert.Empty(inaccessible.Items);
        var accessOnly = await repository.GetPagedAsync(fixture.EmployeeId, null, false, null, null, null, 1, 20);
        Assert.Equal(fixture.Invoices[23].Id, Assert.Single(accessOnly.Items).Id);
        var noScope = await repository.GetPagedAsync(Guid.NewGuid(), null, false, null, null, null, 1, 20);
        Assert.Empty(noScope.Items);
        Assert.Equal(0, noScope.TotalCount);
    }

    private sealed class InvoiceDatabase : IAsyncDisposable
    {
        private readonly string schema = "invoice_read_test_" + Guid.NewGuid().ToString("N");
        private string connection = null!;
        public Guid EmployeeId { get; private set; }
        public Guid PrimaryStoreId { get; private set; }
        public List<Invoice> Invoices { get; } = [];

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

        public static async Task<InvoiceDatabase> CreateAsync()
        {
            var fixture = new InvoiceDatabase();
            fixture.connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POS_TEST_POSTGRES"))
            { SearchPath = fixture.schema, Pooling = false }.ConnectionString;
            await using var admin = new NpgsqlConnection(fixture.connection);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE SCHEMA \"{fixture.schema}\"", admin);
            await create.ExecuteNonQueryAsync();
            try
            {
                await using var db = fixture.Db();
                await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
                var stores = new[] { new Store("Primary"), new Store("Granted"), new Store("Other") };
                var role = new Role(RoleNames.Cashier, true);
                var employee = new Employee("Reader", "reader", "unused", "unused", role.Id, storeId: stores[0].Id);
                employee.setPinLookUpHash(new string('b', 64));
                fixture.EmployeeId = employee.Id;
                fixture.PrimaryStoreId = stores[0].Id;
                db.AddRange(stores);
                db.AddRange(role, employee);
                var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
                for (var storeIndex = 0; storeIndex < stores.Length; storeIndex++)
                {
                    var store = stores[storeIndex];
                    var shift = Shift.Open(store.Id, employee.Id, 0, null);
                    var category = Category.Create(store.Id, "Category");
                    var product = new Product(store.Id, category.Id, $"Product-{storeIndex}", "piece");
                    var sku = new Sku(product.Id, store.Id, $"SKU-{storeIndex}", $"BARCODE-{storeIndex}", 100, 50, 10, true)
                        { Product = product };
                    db.AddRange(shift, category, product, sku);
                    var count = storeIndex == 0 ? 23 : 1;
                    for (var index = 0; index < count; index++)
                    {
                        var order = Order.CreateDraft(store.Id, shift.Id, employee.Id);
                        order.AddOrUpdateItem(sku, 2).ApplyDiscountAndTax(20, 10);
                        var invoice = Invoice.Create(order.Id, $"HD-{store.Code}-20260901-{index + 1:D6}",
                            200, 18, 198, "Buyer", "TAX", "Address");
                        var day = fixture.Invoices.Count == 1 ? 0 : fixture.Invoices.Count;
                        typeof(Invoice).GetProperty(nameof(Invoice.IssuedAt))!.SetValue(invoice, start.AddDays(day));
                        fixture.Invoices.Add(invoice);
                        db.AddRange(order, invoice);
                    }
                }
                await db.SaveChangesAsync();
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }

        public async ValueTask DisposeAsync()
        {
            await using var admin = new NpgsqlConnection(connection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
