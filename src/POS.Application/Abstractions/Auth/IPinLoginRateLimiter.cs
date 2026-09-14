namespace POS.Application.Abstractions.Auth;

public interface IPinLoginRateLimiter
{
  Task<bool> IsBlockedAsync(Guid storeId, string deviceId, CancellationToken cancellationToken = default);
  Task RegisterFailedAttemptAsync(Guid storeId, string deviceId, CancellationToken cancellationToken = default);
  Task ResetAsync(Guid storeId, string deviceId, CancellationToken cancellationToken = default);
}