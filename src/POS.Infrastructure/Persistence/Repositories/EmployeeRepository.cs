using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;

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
}