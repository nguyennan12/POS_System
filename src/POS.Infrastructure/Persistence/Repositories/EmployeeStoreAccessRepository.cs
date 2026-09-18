using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;

namespace POS.Infrastructure.Persistence.Repositories;

public class EmployeeStoreAccessRepository(AppDbContext context) : IEmployeeStoreAccessRepository
{
    public async Task RemoveAllByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await context.EmployeeStoreAccesses.Where(a => a.EmployeeId == employeeId).ToListAsync(cancellationToken);
        context.EmployeeStoreAccesses.RemoveRange(rows);
    }

    public Task<bool> ExistsAsync(Guid employeeId, Guid storeId, CancellationToken cancellationToken = default) =>
        context.EmployeeStoreAccesses.AnyAsync(a => a.EmployeeId == employeeId && a.StoreId == storeId, cancellationToken);

    public async Task AddAsync(EmployeeStoreAccess access, CancellationToken cancellationToken = default) =>
        await context.EmployeeStoreAccesses.AddAsync(access, cancellationToken);
}
