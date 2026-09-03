using System.Text.Json;
using POS.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace POS.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(key);
        if (!value.HasValue) return default;
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await Db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        Db.KeyDeleteAsync(key);
}
