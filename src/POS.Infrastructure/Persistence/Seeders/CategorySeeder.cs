using Microsoft.EntityFrameworkCore;
using POS.Domain.Products;

namespace POS.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the default category tree (7 root categories + 22 child categories = 29 total)
/// for the default store. Must run after StoreAndEmployeeSeeder.
/// Idempotent: skips if any categories already exist for the store.
/// </summary>
public class CategorySeeder : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var defaultStoreId = await context.Stores
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultStoreId == Guid.Empty)
            return;

        // Idempotent guard: skip if categories already exist for this store
        if (await context.Categories.AnyAsync(c => c.StoreId == defaultStoreId, cancellationToken))
            return;

        var categories = new List<Category>();
        int rootOrder = 1;

        Category Root(string name)
        {
            var cat = Category.Create(defaultStoreId, name, parentId: null, displayOrder: rootOrder++);
            categories.Add(cat);
            return cat;
        }

        void Child(Guid parentId, string name, int order)
        {
            var cat = Category.Create(defaultStoreId, name, parentId: parentId, displayOrder: order);
            categories.Add(cat);
        }

        // 1. Đồ uống (3 children)
        var doUong = Root("Đồ uống");
        Child(doUong.Id, "Nước suối", 1);
        Child(doUong.Id, "Nước ngọt", 2);
        Child(doUong.Id, "Cà phê và trà", 3);

        // 2. Thực phẩm khô (4 children)
        var thucPhamKho = Root("Thực phẩm khô");
        Child(thucPhamKho.Id, "Mì và cháo", 1);
        Child(thucPhamKho.Id, "Đồ hộp", 2);
        Child(thucPhamKho.Id, "Gia vị", 3);
        Child(thucPhamKho.Id, "Ngũ cốc & hạt ăn liền", 4);

        // 3. Bánh kẹo và ăn vặt (3 children)
        var banhKeo = Root("Bánh kẹo và ăn vặt");
        Child(banhKeo.Id, "Bánh snack", 1);
        Child(banhKeo.Id, "Kẹo", 2);
        Child(banhKeo.Id, "Socola", 3);

        // 4. Sữa và dinh dưỡng (3 children)
        var sua = Root("Sữa và dinh dưỡng");
        Child(sua.Id, "Sữa nước", 1);
        Child(sua.Id, "Sữa chua", 2);
        Child(sua.Id, "Sữa bột & sữa hạt", 3);

        // 5. Chăm sóc cá nhân (3 children)
        var chamSoc = Root("Chăm sóc cá nhân");
        Child(chamSoc.Id, "Dầu gội", 1);
        Child(chamSoc.Id, "Vệ sinh răng miệng", 2);
        Child(chamSoc.Id, "Chăm sóc da", 3);

        // 6. Kem & đồ đông lạnh (3 children)
        var kemDongLanh = Root("Kem & đồ đông lạnh");
        Child(kemDongLanh.Id, "Kem que & kem hộp", 1);
        Child(kemDongLanh.Id, "Đá viên", 2);
        Child(kemDongLanh.Id, "Thực phẩm đông lạnh", 3);

        // 7. Đồ dùng gia đình nhỏ (3 children)
        var doDung = Root("Đồ dùng gia đình nhỏ");
        Child(doDung.Id, "Túi rác & màng bọc thực phẩm", 1);
        Child(doDung.Id, "Khăn giấy & giấy vệ sinh", 2);
        Child(doDung.Id, "Bao cao su & sản phẩm sức khỏe", 3);

        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
