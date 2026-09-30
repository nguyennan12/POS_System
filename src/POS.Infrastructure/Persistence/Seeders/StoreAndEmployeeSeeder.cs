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


    var ownerRoleId = await context.Roles
        .Where(r => r.StoreId == null && r.Name == RoleNames.Owner)
        .Select(r => r.Id)
        .FirstAsync(cancellationToken);

    var storeManagerRoleId = await context.Roles
        .Where(r => r.StoreId == null && r.Name == RoleNames.StoreManager)
        .Select(r => r.Id)
        .FirstAsync(cancellationToken);

    var cashierRoleId = await context.Roles
        .Where(r => r.StoreId == null && r.Name == RoleNames.Cashier)
        .Select(r => r.Id)
        .FirstAsync(cancellationToken);

    // 1. Seed Default Store (Cửa Hàng Trung Tâm)
    var defaultStore = await context.Stores.FirstOrDefaultAsync(s => s.Name == "Cửa Hàng Trung Tâm", cancellationToken);
    if (defaultStore == null)
    {
      var defaultStoreId = Guid.NewGuid();
      defaultStore = new Store(
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

      await context.Stores.AddAsync(defaultStore, cancellationToken);
      await context.SaveChangesAsync(cancellationToken);
    }

    // 2. Seed Second Store (Chi Nhánh Quận 3)
    if (!await context.Stores.AnyAsync(s => s.Name == "Chi Nhánh Quận 3", cancellationToken))
    {
      var secondStore = new Store(
          "Chi Nhánh Quận 3",
          "456 Đường Lê Văn Sỹ, Quận 3, TP. Hồ Chí Minh",
          "0909876543",
          "Asia/Ho_Chi_Minh",
          "VND",
          "0109876543",
          "Chào mừng quý khách đến với OraPOS!",
          "Cảm ơn và hẹn gặp lại quý khách!",
          true,
          Guid.NewGuid());

      await context.Stores.AddAsync(secondStore, cancellationToken);
      await context.SaveChangesAsync(cancellationToken);
    }

    var employeesToAdd = new List<Employee>();

    // 2. Seed Owner Employee
    if (!await context.Employees.AnyAsync(e => e.Username == "owner", cancellationToken))
    {
      var ownerUser = new Employee(
          "Chủ Hệ Thống (Owner)",
          "owner",
          BCrypt.Net.BCrypt.HashPassword("Owner@123"),
          BCrypt.Net.BCrypt.HashPassword("888888"),
          ownerRoleId,
          isChainOwner: true,
          storeId: defaultStore.Id,
          isActive: true,
          id: Guid.NewGuid());

      ownerUser.setPinLookUpHash(_pinLookupHasher.ComputeHash("888888"));
      employeesToAdd.Add(ownerUser);
    }

    // 3. Seed Store Manager Employee
    if (!await context.Employees.AnyAsync(e => e.Username == "store_manager", cancellationToken))
    {
      var managerUser = new Employee(
          "Cửa Hàng Trưởng",
          "store_manager",
          BCrypt.Net.BCrypt.HashPassword("Manager@123"),
          BCrypt.Net.BCrypt.HashPassword("123456"),
          storeManagerRoleId,
          isChainOwner: false,
          storeId: defaultStore.Id,
          isActive: true,
          id: Guid.NewGuid());

      managerUser.setPinLookUpHash(_pinLookupHasher.ComputeHash("123456"));
      employeesToAdd.Add(managerUser);
    }

    // 4. Seed Cashier Employee
    if (!await context.Employees.AnyAsync(e => e.Username == "cashier01", cancellationToken))
    {
      var cashierUser = new Employee(
          "Thu Ngân 01",
          "cashier01",
          BCrypt.Net.BCrypt.HashPassword("Cashier@123"),
          BCrypt.Net.BCrypt.HashPassword("654321"),
          cashierRoleId,
          isChainOwner: false,
          storeId: defaultStore.Id,
          isActive: true,
          id: Guid.NewGuid());

      cashierUser.setPinLookUpHash(_pinLookupHasher.ComputeHash("654321"));
      employeesToAdd.Add(cashierUser);
    }

    if (employeesToAdd.Count > 0)
    {
      await context.Employees.AddRangeAsync(employeesToAdd, cancellationToken);
      await context.SaveChangesAsync(cancellationToken);
    }
  }
}