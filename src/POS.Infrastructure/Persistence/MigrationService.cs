using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence;

public class MigrationService : IMigrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(AppDbContext context, ILogger<MigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database migration process...");
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migration completed successfully.");

            _logger.LogInformation("Checking and running seeders...");
            await SeedRolesAsync(cancellationToken);
            await SeedStoresAndEmployeesAsync(cancellationToken);
            
            // Dễ dàng thêm các seeder tiếp theo ở đây khi phát triển thêm module (ví dụ: SeedCategoriesAndProductsAsync)
            _logger.LogInformation("All seeders executed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database migration and seeding.");
            throw;
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Role data already exists. Skipping role seeder.");
            return;
        }

        var roles = new[]
        {
            new Role("Owner", isSystemRole: true),
            new Role("Admin", isSystemRole: true),
            new Role("Manager", isSystemRole: true),
            new Role("Cashier", isSystemRole: true)
        };

        await _context.Roles.AddRangeAsync(roles, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Default system roles seeded successfully.");
    }

    private async Task SeedStoresAndEmployeesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Stores.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Store data already exists. Skipping Store & Employee seeder.");
            return;
        }

        _logger.LogInformation("Seeding default store and initial employee accounts...");

        var adminRoleId = await _context.Roles
            .Where(r => r.StoreId == null && r.Name == "Admin")
            .Select(r => r.Id)
            .FirstAsync(cancellationToken);

        var cashierRoleId = await _context.Roles
            .Where(r => r.StoreId == null && r.Name == "Cashier")
            .Select(r => r.Id)
            .FirstAsync(cancellationToken);

        var defaultStoreId = Guid.NewGuid();
        var defaultStore = new Store(
            "Cửa Hàng Trung Tâm",
            "123 Đường Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
            "0901234567",
            "Asia/Ho_Chi_Minh",
            "VND",
            "0101234567",
            "Chào mừng quý khách đến với POS System!",
            "Cảm ơn và hẹn gặp lại quý khách!",
            true,
            defaultStoreId);

        var adminUser = new Employee(
            "Quản Trị Viên",
            "admin",
            BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            BCrypt.Net.BCrypt.HashPassword("123456"),
            adminRoleId,
            isActive: true,
            storeId: defaultStoreId,
            id: Guid.NewGuid());

        var cashierUser = new Employee(
            "Thu Ngân 01",
            "cashier01",
            BCrypt.Net.BCrypt.HashPassword("Cashier@123"),
            BCrypt.Net.BCrypt.HashPassword("654321"),
            cashierRoleId,
            isActive: true,
            storeId: defaultStoreId,
            id: Guid.NewGuid());

        await _context.Stores.AddAsync(defaultStore, cancellationToken);
        await _context.Employees.AddRangeAsync(new[] { adminUser, cashierUser }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Default store and 2 initial employee accounts (admin & cashier01) seeded successfully.");
    }
}
