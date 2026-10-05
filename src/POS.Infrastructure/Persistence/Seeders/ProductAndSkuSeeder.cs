using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Auth;
using POS.Domain.Products;
using POS.Domain.Products.Enums;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Seeders;

public class ProductAndSkuSeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(cancellationToken))
            return;

        var store = await context.Stores.FirstOrDefaultAsync(cancellationToken);
        if (store == null) return;

        var category = await context.Categories.FirstOrDefaultAsync(cancellationToken);
        if (category == null)
        {
            category = Category.Create(store.Id, "Nước Uống");
            await context.Categories.AddAsync(category, cancellationToken);
        }

        var product = new Product(
            store.Id,
            category.Id,
            "Coca Cola",
            "Nước ngọt có gas",
            "Coca-Cola",
            "Lon",
            null,
            ProductStatus.Active,
            Guid.NewGuid()
        );

        var sku = new Sku(
            product.Id,
            store.Id,
            "COCA01",
            "1234567890123", // Barcode to test
            10000m,
            8000m,
            10m,
            true,
            null,
            Guid.NewGuid()
        );

        var product2 = new Product(
            store.Id,
            category.Id,
            "Áo Thun Trơn",
            "Áo thun cotton 100%",
            "Local Brand",
            "Cái",
            null,
            ProductStatus.Active,
            Guid.NewGuid()
        );

        var sku2 = new Sku(
            product2.Id,
            store.Id,
            "AOT01-M-DEN",
            "SP00001", // Barcode to test
            150000m,
            100000m,
            10m,
            true,
            null,
            Guid.NewGuid()
        );

        await context.Products.AddRangeAsync(new[] { product, product2 }, cancellationToken);
        await context.Skus.AddRangeAsync(new[] { sku, sku2 }, cancellationToken);
        
        await context.SaveChangesAsync(cancellationToken);
    }
}
