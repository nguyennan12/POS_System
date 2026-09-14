using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;

namespace POS.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
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