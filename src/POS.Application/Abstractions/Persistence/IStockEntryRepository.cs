using POS.Domain.Inventory.Stock;

namespace POS.Application.Abstractions.Persistence;

public interface IStockEntryRepository
{
    Task<StockEntry?> GetBySkuAndStoreAsync(Guid skuId, Guid storeId, CancellationToken cancellationToken = default);
    Task DeductStockAsync(Guid skuId, Guid storeId, decimal qty, CancellationToken cancellationToken = default);
}
