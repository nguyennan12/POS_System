using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
  Task AddAsync(
      RefreshToken refreshToken,
      CancellationToken cancellationToken = default);
}