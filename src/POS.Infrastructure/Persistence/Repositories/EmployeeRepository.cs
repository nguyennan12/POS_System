using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Employee?> GetByStoreAndPinLookupHashWithRoleAndStoreAsync(Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default)
    {
        return _context.Employees
              .Include(e => e.Role)
              .Include(e => e.Store)
              .FirstOrDefaultAsync(
                e => e.PinLookupHash == pinLookupHash && e.StoreId == storeId,
                cancellationToken
              );
    }

    public Task<Employee?> GetByUsernameWithRoleAndStoreAsync(string username, CancellationToken cancellationToken = default)
    {
        return _context.Employees
              .Include(e => e.Role)
              .Include(e => e.Store)
              .FirstOrDefaultAsync(e => e.Username == username, cancellationToken);
    }

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Employees
              .Include(e => e.Role)
              .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<bool> HasPinConflictAsync(Guid employeeId, Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(
          e => e.Id != employeeId && e.StoreId == storeId && e.PinLookupHash == pinLookupHash,
          cancellationToken
        );
    }

    public Task<bool> HasMissingPinLookupAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(
          e => e.StoreId == storeId && (e.PinLookupHash == null || e.PinLookupHash.Trim() == ""),
          cancellationToken
        );
    }

    public Task<bool> HasOpenShiftOutsideStoreAsync(Guid employeeId, Guid storeId, CancellationToken cancellationToken = default)
    {
        return _context.Shifts.AnyAsync(
          s => s.EmployeeId == employeeId && s.StoreId != storeId && s.Status == ShiftStatus.Open,
          cancellationToken
        );
    }
}