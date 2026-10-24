using Microsoft.EntityFrameworkCore;
using Npgsql;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Stores;

namespace POS.Infrastructure.Tests.Invoices;

public class ReceiptPersistenceTests
{
    [PostgresFact]
    public async Task GetForReceipt_LoadsStoreOriginalCashierItemsAndPaymentsWithoutTracking()
    {
        var schema = "receipt_test_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POS_TEST_POSTGRES"))
        { SearchPath = schema, Pooling = false }.ConnectionString;
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
            var store = new Store("Cửa hàng", receiptHeader: "Xin chào", receiptFooter: "Cảm ơn");
            var role = new Role(RoleNames.Cashier, true);
            var cashier = new Employee("Thu ngân gốc", "cashier", "hash", "hash", role.Id, storeId: store.Id);
            var register = new PosRegister(store.Id, "Quầy 01", "POS-01");
            var shift = Shift.Open(store.Id, register.Id, cashier.Id, 0);
            var category = Category.Create(store.Id, "Đồ uống");
            var product = new Product(store.Id, category.Id, "Cà phê", "ly");
            var sku = new Sku(product.Id, store.Id, "CAFE", "123", 100, 50, 0, true) { Product = product };
            var order = Order.CreateDraft(store.Id, shift.Id, cashier.Id);
            order.AddOrUpdateItem(sku, 1);
            var payment = Payment.CreateSuccess(order.Id, PaymentMethod.Cash, 200, changeAmount: 100);
            var invoice = Invoice.Create(order.Id, "HD-TEST-20261024-000001", 100, 0, 100);
            await using (var seed = new AppDbContext(options))
            {
                await seed.Database.ExecuteSqlRawAsync(seed.Database.GenerateCreateScript());
                seed.AddRange(store, role, cashier, register, shift, category, product, sku, order, payment, invoice);
                await seed.SaveChangesAsync();
            }
            await using var db = new AppDbContext(options);
            var loaded = await new InvoiceRepository(db).GetForReceiptAsync(invoice.Id);
            Assert.NotNull(loaded);
            Assert.Equal(store.Id, loaded.Order.Store.Id);
            Assert.Equal("Xin chào", loaded.Order.Store.ReceiptHeader);
            Assert.Equal(cashier.Id, loaded.Order.CreatedByEmployee.Id);
            Assert.Equal("Cà phê", Assert.Single(loaded.Order.Items).Sku.Product.Name);
            Assert.Equal(payment.Id, Assert.Single(loaded.Order.Payments).Id);
            Assert.Empty(db.ChangeTracker.Entries());
            Assert.Equal(1, await db.Invoices.CountAsync());
            Assert.Empty(await db.InvoiceSequences.ToListAsync());
            Assert.Null(await new InvoiceRepository(db).GetForReceiptAsync(Guid.NewGuid()));
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
