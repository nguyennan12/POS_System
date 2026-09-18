using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Rbac.Constants;

namespace POS.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await _context.Employees.AddAsync(employee, cancellationToken);

    public async Task<Employee?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // AuthorizationBehavior may have loaded the caller before the transaction began.
        // Refresh that tracked row so hierarchy checks use this transaction's snapshot.
        var tracked = _context.Employees.Local.FirstOrDefault(e => e.Id == id);
        if (tracked != null)
        {
            var entry = _context.Entry(tracked);
            await entry.ReloadAsync(cancellationToken);
            if (entry.State == EntityState.Detached) return null;
            await entry.Reference(e => e.Role).Query().LoadAsync(cancellationToken);
            await entry.Reference(e => e.Store).Query().LoadAsync(cancellationToken);
            return tracked;
        }
        return await _context.Employees.Include(e => e.Role).Include(e => e.Store)
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(e => EF.Property<string>(e, "NormalizedUsername") == username.Trim().ToLower(), cancellationToken);

    public Task<int> CountActiveOwnersInStoreAsync(Guid? storeId, CancellationToken cancellationToken = default) =>
        _context.Employees.CountAsync(e => e.StoreId == storeId && e.IsActive &&
            (e.IsChainOwner || (e.Role.StoreId == null && e.Role.Name == RoleNames.Owner)), cancellationToken);

    public async Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(Guid? storeId, Guid? employeeId,
        Guid? roleId, string? search, bool? isActive, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.AsNoTracking().AsQueryable();
        if (storeId.HasValue) query = query.Where(e => e.StoreId == storeId);
        if (employeeId.HasValue) query = query.Where(e => e.Id == employeeId);
        if (roleId.HasValue) query = query.Where(e => e.RoleId == roleId);
        if (isActive.HasValue) query = query.Where(e => e.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term) || e.Username.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Include(e => e.Role).Include(e => e.Store)
            .OrderBy(e => e.Name).ThenBy(e => e.Id).Skip((pageNumber - 1) * pageSize)
            .Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
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
