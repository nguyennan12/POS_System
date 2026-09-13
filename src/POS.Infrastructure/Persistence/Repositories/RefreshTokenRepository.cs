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
}