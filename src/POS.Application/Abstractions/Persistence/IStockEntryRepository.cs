using POS.Domain.Inventory.Stock;

namespace POS.Application.Abstractions.Persistence;

public interface IStockEntryRepository
{
    Task<StockEntry?> GetBySkuAndStoreAsync(Guid skuId, Guid storeId, CancellationToken cancellationToken = default);

    Task<(List<StockEntry> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        Guid? skuId,
        Guid? categoryId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Trả về danh sách tồn kho cần cảnh báo: min-stock hoặc hàng cận hạn. Cũng trả về tập SkuId near-expiry.</summary>
    Task<(List<StockEntry> Items, int TotalCount, HashSet<Guid> NearExpirySkuIds)> GetAlertsAsync(
        Guid? storeId,
        int nearExpiryDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(List<StockBatch> Items, int TotalCount)> GetBatchesAsync(
        Guid? storeId,
        Guid? skuId,
        DateOnly? expiryBefore,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Deduct (trừ) tồn kho bằng raw SQL atomic để tránh lost-update.</summary>
    Task DeductStockAsync(Guid skuId, Guid storeId, decimal qty, CancellationToken cancellationToken = default);

    /// <summary>Trừ tổng tồn kho và tự động trừ các lô còn hàng theo thứ tự hạn dùng gần nhất (FEFO).</summary>
    Task DeductStockWithBatchesAsync(Guid skuId, Guid storeId, decimal qty, CancellationToken cancellationToken = default);

    /// <summary>Cộng tồn kho và cập nhật giá vốn bình quân bằng raw SQL atomic.</summary>
    Task IncrementStockAsync(Guid skuId, Guid storeId, decimal qty, decimal newAverageCost, CancellationToken cancellationToken = default);
}

