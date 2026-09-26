using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Stock;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockTransactionRepository(AppDbContext dbContext) : IStockTransactionRepository
{
    public async Task AddAsync(StockTransaction transaction, CancellationToken cancellationToken = default)
    {
        await dbContext.StockTransactions.AddAsync(transaction, cancellationToken);
    }
}
