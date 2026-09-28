using POS.Domain.Inventory.Stock;

namespace POS.Application.Abstractions.Persistence;

public interface IStockTransactionRepository
{
    Task AddAsync(StockTransaction transaction, CancellationToken cancellationToken = default);

    Task<(List<StockTransaction> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        Guid? skuId,
        string? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

