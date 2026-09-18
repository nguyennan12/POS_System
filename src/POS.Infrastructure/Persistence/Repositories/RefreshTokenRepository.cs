using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;

namespace POS.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
  public async Task RevokeAllByEmployeeIdAsync(Guid employeeId, DateTime utcNow, CancellationToken cancellationToken = default)
  {
    var tokens = await _context.RefreshTokens.Where(t => t.EmployeeId == employeeId &&
        t.RevokedAt == null && t.ExpiresAt > utcNow).ToListAsync(cancellationToken);
    foreach (var token in tokens) token.Revoke(utcNow);
  }

  private readonly AppDbContext _context;

  public RefreshTokenRepository(AppDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
  {
    await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
  }

  public Task<RefreshToken?> GetByTokenHashWithEmployeeAsync(string tokenHash, CancellationToken cancellationToken = default)
  {
    return _context.RefreshTokens
     .Include(r => r.Employee)
     .ThenInclude(e => e.Role)
     .Include(r => r.Employee)
     .ThenInclude(e => e.Store)
     .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

  }
}
