using POS.Domain.Inventory.Stock;

namespace POS.Application.Abstractions.Persistence;

public interface IStockTransactionRepository
{
    Task AddAsync(StockTransaction transaction, CancellationToken cancellationToken = default);
}
