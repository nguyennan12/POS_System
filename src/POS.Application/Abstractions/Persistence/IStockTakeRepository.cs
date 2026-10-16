using POS.Domain.Inventory.StockTake;

namespace POS.Application.Abstractions.Persistence;

public interface IStockTakeRepository
{
    Task<StockTake?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<StockTake> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockTake stockTake, CancellationToken cancellationToken = default);
}
