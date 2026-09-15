using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> HasPinConflictAsync(Guid employeeId, Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default) =>
        context.Employees.AnyAsync(e => e.Id != employeeId && e.StoreId == storeId && e.PinLookupHash == pinLookupHash, cancellationToken);

    public Task<bool> HasOpenShiftOutsideStoreAsync(Guid employeeId, Guid storeId, CancellationToken cancellationToken = default) =>
        context.Shifts.AnyAsync(s => s.EmployeeId == employeeId && s.StoreId != storeId && s.Status == ShiftStatus.Open, cancellationToken);

    public Task<bool> HasMissingPinLookupAsync(Guid storeId, CancellationToken cancellationToken = default) =>
        context.Employees.AnyAsync(e => e.StoreId == storeId &&
            (e.PinLookupHash == null || e.PinLookupHash.Trim() == ""), cancellationToken);
}
