using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Auth;
using POS.Domain.Employees;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Seeders;

public class StoreAndEmployeeSeeder : ISeeder
{
  private readonly IPinLookupHasher _pinLookupHasher;

  public StoreAndEmployeeSeeder(IPinLookupHasher pinLookupHasher)
  {
    _pinLookupHasher = pinLookupHasher;
  }
  public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
  {


    if (await context.Stores.AnyAsync(cancellationToken))
      return;


    var storeManagerRoleId = await context.Roles
        .Where(r => r.StoreId == null && r.Name == RoleNames.StoreManager)
        .Select(r => r.Id)
        .FirstAsync(cancellationToken);

    var cashierRoleId = await context.Roles
        .Where(r => r.StoreId == null && r.Name == RoleNames.Cashier)
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

    var managerUser = new Employee(
        "Cửa Hàng Trưởng",
        "store_manager",
        BCrypt.Net.BCrypt.HashPassword("Manager@123"),
        BCrypt.Net.BCrypt.HashPassword("123456"),
        storeManagerRoleId,
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

    managerUser.setPinLookUpHash(_pinLookupHasher.ComputeHash("123456"));
    cashierUser.setPinLookUpHash(_pinLookupHasher.ComputeHash("654321"));

    await context.Stores.AddAsync(defaultStore, cancellationToken);
    await context.Employees.AddRangeAsync(new[] { managerUser, cashierUser }, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
  }
}