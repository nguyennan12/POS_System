using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;

using POS.Infrastructure.Tests.Stores;

namespace POS.Infrastructure.Tests.Repositories;

public class ReportRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [PostgresFact]
    [Trait("Category", "PostgreSQL")]
    public async Task GetRevenueReportAsync_ShouldAggregateMetricsAndGroupByDate()
    {
        // Arrange
        await using var db = CreateDbContext();
        var shiftRepo = Substitute.For<IShiftRepository>();
        var repo = new ReportRepository(db, shiftRepo);

        var storeId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();

        var paidDate1 = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        var paidDate2 = new DateTime(2026, 10, 11, 14, 0, 0, DateTimeKind.Utc);

        // Order 1 (Paid)
        var order1 = Order.CreateDraft(storeId, shiftId, empId);
        typeof(Order).GetProperty("PaidAt")!.SetValue(order1, paidDate1);
        typeof(Order).GetProperty("Subtotal")!.SetValue(order1, 100_000m);
        typeof(Order).GetProperty("DiscountTotal")!.SetValue(order1, 10_000m);
        typeof(Order).GetProperty("TaxTotal")!.SetValue(order1, 9_000m);
        typeof(Order).GetProperty("GrandTotal")!.SetValue(order1, 99_000m);

        // Order 2 (Paid, same day)
        var order2 = Order.CreateDraft(storeId, shiftId, empId);
        typeof(Order).GetProperty("PaidAt")!.SetValue(order2, paidDate1.AddHours(2));
        typeof(Order).GetProperty("Subtotal")!.SetValue(order2, 200_000m);
        typeof(Order).GetProperty("DiscountTotal")!.SetValue(order2, 20_000m);
        typeof(Order).GetProperty("TaxTotal")!.SetValue(order2, 18_000m);
        typeof(Order).GetProperty("GrandTotal")!.SetValue(order2, 198_000m);

        // Order 3 (Paid, next day)
        var order3 = Order.CreateDraft(storeId, shiftId, empId);
        typeof(Order).GetProperty("PaidAt")!.SetValue(order3, paidDate2);
        typeof(Order).GetProperty("Subtotal")!.SetValue(order3, 300_000m);
        typeof(Order).GetProperty("DiscountTotal")!.SetValue(order3, 0m);
        typeof(Order).GetProperty("TaxTotal")!.SetValue(order3, 30_000m);
        typeof(Order).GetProperty("GrandTotal")!.SetValue(order3, 330_000m);

        // Order 4 (Cancelled - should be excluded)
        var order4 = Order.CreateDraft(storeId, shiftId, empId);
        typeof(Order).GetProperty("Status")!.SetValue(order4, OrderStatus.Cancelled);
        typeof(Order).GetProperty("PaidAt")!.SetValue(order4, paidDate2);
        typeof(Order).GetProperty("GrandTotal")!.SetValue(order4, 500_000m);

        // Order 5 (Draft / Not Paid - should be excluded)
        var order5 = Order.CreateDraft(storeId, shiftId, empId);

        db.Orders.AddRange(order1, order2, order3, order4, order5);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.GetRevenueReportAsync(
            from: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
            storeId: storeId,
            employeeId: empId);

        // Assert
        result.TotalOrders.Should().Be(3);
        result.TotalRevenue.Should().Be(600_000m); // 100k + 200k + 300k
        result.TotalDiscount.Should().Be(30_000m); // 10k + 20k
        result.TotalTax.Should().Be(57_000m); // 9k + 18k + 30k
        result.NetRevenue.Should().Be(627_000m); // 99k + 198k + 330k

        result.Periods.Should().HaveCount(2);
        var day1 = result.Periods.First(p => p.Period == "2026-10-10");
        day1.OrderCount.Should().Be(2);
        day1.Revenue.Should().Be(300_000m);
        day1.NetRevenue.Should().Be(297_000m);

        var day2 = result.Periods.First(p => p.Period == "2026-10-11");
        day2.OrderCount.Should().Be(1);
        day2.Revenue.Should().Be(300_000m);
        day2.NetRevenue.Should().Be(330_000m);
    }

    [PostgresFact]
    [Trait("Category", "PostgreSQL")]
    public async Task GetInventoryReportAsync_ShouldCalculateMetricsCorrectly()
    {
        // Arrange
        await using var db = CreateDbContext();
        var shiftRepo = Substitute.For<IShiftRepository>();
        var repo = new ReportRepository(db, shiftRepo);

        var storeId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var category = Category.Create(storeId, "Nước ngọt");
        typeof(Category).GetProperty("Id")!.SetValue(category, catId);

        var product = new Product(storeId, catId, "Pepsi", "Lon", id: Guid.NewGuid());
        var sku1 = new Sku(product.Id, storeId, "PEPSI-LON", "89312345", 10_000m, costPrice: 7_000m, id: Guid.NewGuid());
        sku1.Product = product;

        var product2 = new Product(storeId, catId, "Coca", "Lon", id: Guid.NewGuid());
        var sku2 = new Sku(product2.Id, storeId, "COCA-LON", "89312346", 10_000m, costPrice: 6_000m, id: Guid.NewGuid());
        sku2.Product = product2;

        var entry1 = new StockEntry(storeId, sku1.Id, qtyOnHand: 10, minStock: 5);
        typeof(StockEntry).GetProperty("AverageCost")!.SetValue(entry1, 7_000m);

        var entry2 = new StockEntry(storeId, sku2.Id, qtyOnHand: 3, minStock: 5); // Low stock (3 <= 5)
        typeof(StockEntry).GetProperty("AverageCost")!.SetValue(entry2, 6_000m);

        db.Categories.Add(category);
        db.Products.AddRange(product, product2);
        db.Skus.AddRange(sku1, sku2);
        db.StockEntries.AddRange(entry1, entry2);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.GetInventoryReportAsync(storeId, catId);

        // Assert
        result.TotalSkus.Should().Be(2);
        result.OutOfStockCount.Should().Be(0);
        result.LowStockCount.Should().Be(1);
        result.TotalStockValue.Should().Be(10 * 7_000m + 3 * 6_000m); // 70k + 18k = 88k
        result.Items.Should().HaveCount(2);
    }

    [PostgresFact]
    [Trait("Category", "PostgreSQL")]
    public async Task GetSlowMovingItemsAsync_ShouldIdentifyItemsWithoutRecentSales()
    {
        // Arrange
        await using var db = CreateDbContext();
        var shiftRepo = Substitute.For<IShiftRepository>();
        var repo = new ReportRepository(db, shiftRepo);

        var storeId = Guid.NewGuid();
        var product = new Product(storeId, Guid.NewGuid(), "Món hàng", "Cái");
        var skuActive = new Sku(product.Id, storeId, "SKU-ACTIVE", "111", 50_000m, id: Guid.NewGuid());
        var skuSlow = new Sku(product.Id, storeId, "SKU-SLOW", "222", 50_000m, id: Guid.NewGuid());
        skuActive.Product = product;
        skuSlow.Product = product;

        var entryActive = new StockEntry(storeId, skuActive.Id, qtyOnHand: 10);
        var entrySlow = new StockEntry(storeId, skuSlow.Id, qtyOnHand: 20);

        db.Products.Add(product);
        db.Skus.AddRange(skuActive, skuSlow);
        db.StockEntries.AddRange(entryActive, entrySlow);

        // Order selling active SKU yesterday
        var order = Order.CreateDraft(storeId, Guid.NewGuid(), Guid.NewGuid());
        typeof(Order).GetProperty("PaidAt")!.SetValue(order, DateTime.UtcNow.AddDays(-1));
        var orderItem = new OrderItem(order.Id, skuActive.Id, 1, 50_000m);
        typeof(OrderItem).GetProperty("Order")!.SetValue(orderItem, order);
        db.Orders.Add(order);
        db.OrderItems.Add(orderItem);

        // Order selling slow SKU 45 days ago
        var orderOld = Order.CreateDraft(storeId, Guid.NewGuid(), Guid.NewGuid());
        typeof(Order).GetProperty("PaidAt")!.SetValue(orderOld, DateTime.UtcNow.AddDays(-45));
        var orderItemOld = new OrderItem(orderOld.Id, skuSlow.Id, 1, 50_000m);
        typeof(OrderItem).GetProperty("Order")!.SetValue(orderItemOld, orderOld);
        db.Orders.Add(orderOld);
        db.OrderItems.Add(orderItemOld);

        await db.SaveChangesAsync();

        // Act
        var result = await repo.GetSlowMovingItemsAsync(days: 30, storeId: storeId);

        // Assert
        result.Should().ContainSingle();
        var slowItem = result[0];
        slowItem.SkuCode.Should().Be("SKU-SLOW");
        slowItem.DaysWithoutSale.Should().BeGreaterThanOrEqualTo(44);
    }
}
