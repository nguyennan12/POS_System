using Microsoft.EntityFrameworkCore;
using POS.Domain.Products;

namespace POS.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the standard 13 retail departments matching the retail catalog (docs/san_pham.xlsx)
/// across all stores in the system.
/// Idempotent: safe to run multiple times without duplicating or corrupting existing data.
/// </summary>
public class CategorySeeder : ISeeder
{
    public static readonly (string Name, int Order)[] StandardCategories =
    [
        ("Bánh Kẹo", 1),
        ("Chăm Sóc Bé", 2),
        ("Chăm Sóc Cá Nhân", 3),
        ("Gia vị", 4),
        ("Hóa Phẩm - Tẩy rửa", 5),
        ("Mì - Thực Phẩm Ăn Liền", 6),
        ("Sữa các loại", 7),
        ("Thực Phẩm Chế Biến", 8),
        ("Thực Phẩm Khô", 9),
        ("Thực Phẩm Đông Lạnh", 10),
        ("Đồ Dùng Gia Đình", 11),
        ("Đồ Uống - Giải Khát", 12),
        ("Đồ uống có cồn", 13)
    ];

    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var stores = await context.Stores.ToListAsync(cancellationToken);
        if (stores.Count == 0)
            return;

        foreach (var store in stores)
        {
            var existingCategories = await context.Categories
                .Where(c => c.StoreId == store.Id)
                .ToListAsync(cancellationToken);

            var existingByName = existingCategories
                .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Category>();

            foreach (var (name, order) in StandardCategories)
            {
                if (existingByName.TryGetValue(name, out var existing))
                {
                    // Normalize casing, display order and root parent
                    if (existing.Name != name || existing.DisplayOrder != order || existing.ParentId != null)
                    {
                        existing.Update(name, parentId: null, order, existing.ImageUrl, existing.IsVisible);
                    }
                }
                else
                {
                    toAdd.Add(Category.Create(store.Id, name, parentId: null, displayOrder: order));
                }
            }

            if (toAdd.Count > 0)
            {
                await context.Categories.AddRangeAsync(toAdd, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
