using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace POS.WinUI.Core.Services;

/// <summary>
/// Cache Service lấy cảm hứng từ TanStack Query (React Query)
/// Cung cấp cơ chế In-Memory Cache, Stale-While-Revalidate và Query Invalidation
/// Giúp UI chuyển tab, đổi trang, lọc danh mục tức thì 0ms, không nhấp nháy, siêu mượt.
/// </summary>
public interface IQueryCacheService
{
    /// <summary>
    /// Lấy dữ liệu từ cache nếu có và còn tươi; nếu không thì thực thi fetcher để lấy dữ liệu mới.
    /// Hỗ trợ Stale-While-Revalidate: nếu đã cũ nhưng chưa hết hạn thì trả về cache trước, ngầm refresh sau.
    /// </summary>
    Task<T?> GetOrFetchAsync<T>(
        string queryKey,
        Func<Task<T?>> fetcher,
        TimeSpan? staleTime = null,
        bool forceRefresh = false);

    /// <summary>Xóa cache của 1 key cụ thể</summary>
    void Invalidate(string queryKey);

    /// <summary>Xóa toàn bộ cache có tiền tố bắt đầu bằng prefix (ví dụ: "products", "categories")</summary>
    void InvalidateByPrefix(string prefix);

    /// <summary>Xóa toàn bộ cache trong bộ nhớ</summary>
    void Clear();
}

public sealed class QueryCacheService : IQueryCacheService
{
    private sealed class CacheEntry
    {
        public object? Data { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan StaleTime { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(5);

        public bool IsStale => DateTime.UtcNow - CachedAt > StaleTime;
        public bool IsExpired => DateTime.UtcNow - CachedAt > ExpirationTime;
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<T?> GetOrFetchAsync<T>(
        string queryKey,
        Func<Task<T?>> fetcher,
        TimeSpan? staleTime = null,
        bool forceRefresh = false)
    {
        var effectiveStale = staleTime ?? TimeSpan.FromSeconds(60);

        if (!forceRefresh && _cache.TryGetValue(queryKey, out var entry) && !entry.IsExpired)
        {
            // Nếu dữ liệu vẫn còn tươi, trả về ngay 0ms
            if (!entry.IsStale)
            {
                return (T?)entry.Data;
            }

            // Nếu stale nhưng chưa expired (Stale-While-Revalidate):
            // Trả về data cũ cho UI mượt mà, đồng thời ngầm chạy fetcher cập nhật
            _ = Task.Run(async () =>
            {
                try
                {
                    var fresh = await fetcher();
                    if (fresh != null)
                    {
                        entry.Data = fresh;
                        entry.CachedAt = DateTime.UtcNow;
                    }
                }
                catch
                {
                    // Lỗi background revalidation thì giữ nguyên dữ liệu cache cũ
                }
            });

            return (T?)entry.Data;
        }

        // Cần fetch mới đồng bộ
        var keyLock = _locks.GetOrAdd(queryKey, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync();

        try
        {
            // Kiểm tra lại lần 2 sau khi có lock
            if (!forceRefresh && _cache.TryGetValue(queryKey, out entry) && !entry.IsStale)
            {
                return (T?)entry.Data;
            }

            var result = await fetcher();
            if (result != null)
            {
                _cache[queryKey] = new CacheEntry
                {
                    Data = result,
                    CachedAt = DateTime.UtcNow,
                    StaleTime = effectiveStale,
                    ExpirationTime = TimeSpan.FromMinutes(10)
                };
            }

            return result;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public void Invalidate(string queryKey)
    {
        _cache.TryRemove(queryKey, out _);
    }

    public void InvalidateByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return;

        foreach (var key in _cache.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
