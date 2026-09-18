using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
  Task RevokeAllByEmployeeIdAsync(Guid employeeId, DateTime utcNow, CancellationToken cancellationToken = default);
  Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
  Task<RefreshToken?> GetByTokenHashWithEmployeeAsync(string tokenHash, CancellationToken cancellationToken = default);
}
