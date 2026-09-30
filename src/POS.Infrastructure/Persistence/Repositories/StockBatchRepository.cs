using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Stock;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockBatchRepository(AppDbContext dbContext) : IStockBatchRepository
{
    public async Task<StockBatch?> GetByBatchNoAsync(
        Guid storeId, Guid skuId, string batchNo, CancellationToken cancellationToken = default)
    {
        return await dbContext.StockBatches
            .FirstOrDefaultAsync(b =>
                b.StoreId == storeId &&
                b.SkuId == skuId &&
                b.BatchNo == batchNo,
                cancellationToken);
    }

    public async Task<StockBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.StockBatches
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AddAsync(StockBatch batch, CancellationToken cancellationToken = default)
    {
        await dbContext.StockBatches.AddAsync(batch, cancellationToken);
    }
}
