using POS.Application.Abstractions.Auth;
using StackExchange.Redis;

namespace POS.Infrastructure.Auth;

public class PinLoginRateLimiter : IPinLoginRateLimiter
{
  private const int MaxAttempts = 5;
  private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(5);
  private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
  private readonly IConnectionMultiplexer _redis;

  public PinLoginRateLimiter(IConnectionMultiplexer redis)
  {
    _redis = redis;
  }

  public async Task<bool> IsBlockedAsync(Guid storeId, string deviceId, CancellationToken cancellationToken = default)
  {
    var db = _redis.GetDatabase();
    return await db.KeyExistsAsync(
        GetLockKey(storeId, deviceId));
  }

  public async Task RegisterFailedAttemptAsync(Guid storeId, string deviceId, CancellationToken cancellationToken = default)
  {
    var db = _redis.GetDatabase();
    var attemptsKey = GetAttemptsKey(storeId, deviceId);
    var lockKey = GetLockKey(storeId, deviceId);

    var attempts = await db.StringIncrementAsync(attemptsKey);

    if (attempts == 1)
      await db.KeyExpireAsync(attemptsKey, AttemptWindow);

    if (attempts >= MaxAttempts)
      await db.StringSetAsync(lockKey, "1", LockDuration);
  }

  public async Task ResetAsync(Guid storeId, string deviceId, CancellationToken cancellationToken = default)
  {
    var db = _redis.GetDatabase();
    await db.KeyDeleteAsync(GetAttemptsKey(storeId, deviceId));
    await db.KeyDeleteAsync(GetLockKey(storeId, deviceId));
  }

  private static string GetAttemptsKey(Guid storeId, string deviceId)
  {
    return $"auth:pin-login:attempts:{storeId:N}:{deviceId}";
  }

  private static string GetLockKey(Guid storeId, string deviceId)
  {
    return $"auth:pin-login:lock:{storeId:N}:{deviceId}";
  }
}