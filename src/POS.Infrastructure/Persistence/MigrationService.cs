using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Enums;
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

      // Seeder here
      _logger.LogInformation("Checking and running seeders...");
      await SeedRolesAsync(cancellationToken);
      await SeedResourceAsync(cancellationToken);
      await SeedRolePermissionsAsync(cancellationToken);
      await SeedStoresAndEmployeesAsync(cancellationToken);
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

  private async Task SeedResourceAsync(CancellationToken cancellationToken)
  {
    if (await _context.Resources.AnyAsync(cancellationToken))
    {
      _logger.LogInformation("Resource data already exists. Skipping resource seeder.");
      return;
    }
    // // Seeding Resources
    _logger.LogInformation("Seeding default resource...");


    _logger.LogInformation("Seeding default permission...");

    foreach (var resDef in SystemResources)
    {
      var resource = new Resource(resDef.Code, resDef.Description);
      foreach (var (act, actDesc) in resDef.Actions)
      {
        var permission = new Permission(resource.Id, resource, act, actDesc);
        resource.Permissions.Add(permission);
      }
      await _context.Resources.AddAsync(resource, cancellationToken);
    }
    await _context.SaveChangesAsync(cancellationToken);
    _logger.LogInformation("Resources seeded successfully.");
  }

  private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
  {
    if (await _context.RolePermissions.AnyAsync(cancellationToken))
    {
      _logger.LogInformation("RolePermissions already seeded. Skipping.");
      return;
    }

    var roles = await _context.Roles.ToListAsync(cancellationToken);
    var permissions = await _context.Permissions.Include(p => p.Resource).ToListAsync(cancellationToken);

    var ownerRole = roles.First(r => r.Name == "Owner");
    var adminRole = roles.First(r => r.Name == "Admin");
    var managerRole = roles.First(r => r.Name == "Manager");
    var cashierRole = roles.First(r => r.Name == "Cashier");

    var rolePermissions = new List<RolePermission>();
    // OWNER
    foreach (var p in permissions)
      rolePermissions.Add(new RolePermission(ownerRole.Id, ownerRole, p.Id, p));

    // ADMIN
    var adminPerms = permissions.Where(p =>
        !(p.Resource.Code == "STORES" && (p.Action == PermissionAction.Create || p.Action == PermissionAction.Delete)));
    foreach (var p in adminPerms)
      rolePermissions.Add(new RolePermission(adminRole.Id, adminRole, p.Id, p));

    // MANAGER
    var managerResources = new[] { "ORDERS", "INVENTORY", "CUSTOMERS", "CATEGORIES", "REPORTS" };
    var managerPerms = permissions.Where(p => managerResources.Contains(p.Resource.Code));
    foreach (var p in managerPerms)
      rolePermissions.Add(new RolePermission(managerRole.Id, managerRole, p.Id, p));

    // CASHIER
    var cashierPerms = permissions.Where(p =>
       p.Action == PermissionAction.Read ||
       (p.Resource.Code == "ORDERS" && p.Action == PermissionAction.Create) ||
       (p.Resource.Code == "CUSTOMERS" && p.Action == PermissionAction.Create));
    foreach (var p in cashierPerms)
      rolePermissions.Add(new RolePermission(cashierRole.Id, cashierRole, p.Id, p));

    await _context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);
    _logger.LogInformation("RolePermissions seeded successfully according to architecture specs.");
  }

  private static readonly ResourceDefinition[] SystemResources =
[
  // 1. Store Management
  new("STORES", "Store Management",
    [
        (PermissionAction.Create, "Create new store"),
        (PermissionAction.Read, "View store details and list"),
        (PermissionAction.Update, "Update store information and settings"),
        (PermissionAction.Delete, "Delete store")
    ]),
    // 2. Employee Management
    new("EMPLOYEES", "Employee Management",
    [
        (PermissionAction.Create, "Create new employee account"),
        (PermissionAction.Read, "View employee profiles and list"),
        (PermissionAction.Update, "Update employee information and status"),
        (PermissionAction.Delete, "Delete or deactivate employee account")
    ]),
    // 3. Role & Permission Management
    new("ROLES", "Role and Permission Management",
    [
        (PermissionAction.Create, "Create new custom role"),
        (PermissionAction.Read, "View roles and assigned permission matrix"),
        (PermissionAction.Update, "Update role details and permissions"),
        (PermissionAction.Delete, "Delete custom role")
    ]),
    // 4. Product Management
    new("PRODUCTS", "Product Catalog Management",
    [
        (PermissionAction.Create, "Create new product"),
        (PermissionAction.Read, "View product catalog, pricing, and details"),
        (PermissionAction.Update, "Update product information and pricing"),
        (PermissionAction.Delete, "Delete product")
    ]),
    // 5. Category Management
    new("CATEGORIES", "Category Management",
    [
        (PermissionAction.Create, "Create new product category"),
        (PermissionAction.Read, "View product category list"),
        (PermissionAction.Update, "Update product category details"),
        (PermissionAction.Delete, "Delete product category")
    ]),
    // 6. Inventory Management
    new("INVENTORY", "Inventory and Stock Management",
    [
        (PermissionAction.Create, "Create stock entry or inventory slip"),
        (PermissionAction.Read, "View stock levels and inventory movement"),
        (PermissionAction.Update, "Update inventory quantities and slips"),
        (PermissionAction.Delete, "Delete or void inventory slip")
    ]),
    // 7. Order & Sales Management
    new("ORDERS", "Order and Sales Management",
    [
        (PermissionAction.Create, "Create new sales order"),
        (PermissionAction.Read, "View order details and sales history"),
        (PermissionAction.Update, "Update order status and details"),
        (PermissionAction.Delete, "Cancel or delete sales order")
    ]),
    // 8. Customer Management
    new("CUSTOMERS", "Customer Management",
    [
        (PermissionAction.Create, "Create new customer profile"),
        (PermissionAction.Read, "View customer profiles and purchase history"),
        (PermissionAction.Update, "Update customer details and reward points"),
        (PermissionAction.Delete, "Delete customer profile")
    ]),
    // 9. Discount & Promotion Management
    new("DISCOUNTS", "Discount and Promotion Management",
    [
        (PermissionAction.Create, "Create new promotional campaign or discount"),
        (PermissionAction.Read, "View discount list and active promotions"),
        (PermissionAction.Update, "Update discount rules and validity"),
        (PermissionAction.Delete, "Delete promotional campaign or discount")
    ]),
    // 10. Reports & Analytics Management
    new("REPORTS", "Reports and Analytics Management",
    [
        (PermissionAction.Create, "Generate new analytics report"),
        (PermissionAction.Read, "View sales, revenue, and inventory reports"),
        (PermissionAction.Update, "Update report parameters and settings"),
        (PermissionAction.Delete, "Delete saved report")
    ])
];


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

  private record ResourceDefinition(
    string Code,
    string Description,
    (PermissionAction Action, string Description)[] Actions
    );
}


