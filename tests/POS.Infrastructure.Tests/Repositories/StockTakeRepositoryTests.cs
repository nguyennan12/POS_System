using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.StockTake;
using POS.Domain.Products;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Stores;
using Xunit;

namespace POS.Infrastructure.Tests.Repositories;

public class StockTakeRepositoryTests
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
    public async Task AddAsync_And_GetByIdWithItemsAsync_ShouldPersistAndRetrieveStockTake()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new StockTakeRepository(db);

        var storeId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var product = new Product(storeId, Guid.NewGuid(), "Trà Đào", "Ly");
        var sku = new Sku(product.Id, storeId, "SKU-TD-01", "893111111", sellPrice: 25000m);
        sku.Product = product;

        await db.Products.AddAsync(product);
        await db.Skus.AddAsync(sku);
        await db.SaveChangesAsync();

        var stockTake = StockTake.Create(storeId, empId, "Kiểm kê đầu kỳ");
        stockTake.UpdateItem(sku.Id, actualQty: 50m, systemQty: 45m, note: "Dư 5 ly");

        // Act
        await repository.AddAsync(stockTake);
        await db.SaveChangesAsync();

        var retrieved = await repository.GetByIdWithItemsAsync(stockTake.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(stockTake.Id);
        retrieved.StoreId.Should().Be(storeId);
        retrieved.Status.Should().Be(StockTakeStatus.Draft);
        retrieved.Items.Should().HaveCount(1);

        var item = retrieved.Items.First();
        item.SkuId.Should().Be(sku.Id);
        item.ActualQty.Should().Be(50m);
        item.SystemQty.Should().Be(45m);
        item.DiffQty.Should().Be(5m);
        item.Sku.Should().NotBeNull();
        item.Sku.Product.Should().NotBeNull();
        item.Sku.Product.Name.Should().Be("Trà Đào");
    }

    [PostgresFact]
    [Trait("Category", "PostgreSQL")]
    public async Task GetPagedAsync_ShouldFilterByStoreAndStatus()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new StockTakeRepository(db);

        var store1 = Guid.NewGuid();
        var store2 = Guid.NewGuid();
        var empId = Guid.NewGuid();

        var st1 = StockTake.Create(store1, empId, "Phiếu 1 store 1");
        st1.UpdateItem(Guid.NewGuid(), 10m, 10m);

        var st2 = StockTake.Create(store1, empId, "Phiếu 2 store 1");
        st2.UpdateItem(Guid.NewGuid(), 5m, 5m);
        st2.Submit();

        var st3 = StockTake.Create(store2, empId, "Phiếu 3 store 2");
        st3.UpdateItem(Guid.NewGuid(), 2m, 2m);

        await db.StockTakes.AddRangeAsync(st1, st2, st3);
        await db.SaveChangesAsync();

        // Act - lọc store1 và status Pending
        var (items, total) = await repository.GetPagedAsync(
            storeId: store1,
            status: "Pending",
            from: null,
            to: null,
            pageNumber: 1,
            pageSize: 10);

        // Assert
        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Id.Should().Be(st2.Id);
    }
}
