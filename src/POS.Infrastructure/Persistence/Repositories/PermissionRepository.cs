using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;

namespace POS.Infrastructure.Persistence.Repositories;

public class PermissionRepository : IPermissionRepository
{
  private readonly AppDbContext _context;

  public PermissionRepository(AppDbContext context)
  {
    _context = context;
  }

  public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid roleId, CancellationToken cancellationToken = default)
  {
    return await _context.RolePermissions
        .Where(rp => rp.RoleId == roleId)
        .Select(rp => rp.Permission.Code)
        .ToListAsync(cancellationToken);
  }
}