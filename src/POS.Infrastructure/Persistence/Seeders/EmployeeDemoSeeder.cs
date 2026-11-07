using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Auth;
using POS.Domain.Employees;
using POS.Domain.Rbac.Constants;

namespace POS.Infrastructure.Persistence.Seeders;

// Executed by the Development-only migration pipeline; never overwrites existing employees.
public sealed class EmployeeDemoSeeder(IPinLookupHasher pinHasher) : ISeeder
{
    public async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var stores = await context.Stores.OrderBy(s => s.Name).Take(2).ToListAsync(cancellationToken);
        var cashierRole = await context.Roles.SingleAsync(r => r.StoreId == null && r.Name == RoleNames.Cashier, cancellationToken);
        var managerRole = await context.Roles.SingleAsync(r => r.StoreId == null && r.Name == RoleNames.StoreManager, cancellationToken);
        var names = new[] { "Nguyễn Minh Anh", "Trần Thu Hà", "Lê Quốc Bảo", "Phạm Ngọc Lan", "Hoàng Đức Nam" };
        for (var i = 0; i < names.Length; i++)
        {
            var username = $"demo_employee_{i + 1:00}";
            if (await context.Employees.AnyAsync(e => e.Username == username, cancellationToken)) continue;
            var storeId = stores[i % stores.Count].Id;
            var pin = $"73120{i}";
            var lookup = pinHasher.ComputeHash(pin);
            // Avoid conflicting with an account created by a developer before this seed runs.
            if (await context.Employees.AnyAsync(e => e.PinLookupHash == lookup && (e.StoreId == storeId || e.IsChainOwner), cancellationToken)) continue;
            var employee = new Employee(names[i], username, BCrypt.Net.BCrypt.HashPassword("Demo@123"),
                BCrypt.Net.BCrypt.HashPassword(pin), i == 0 ? managerRole.Id : cashierRole.Id,
                storeId: storeId, isActive: i != 4);
            employee.setPinLookUpHash(lookup);
            employee.UpdateContactDetails($"090123450{i}", $"employee{i + 1}@example.com", null,
                new DateOnly(2026, 1, i + 1), i % 2 == 0 ? "Full-time" : "Part-time", 65000m);
            context.Employees.Add(employee);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
