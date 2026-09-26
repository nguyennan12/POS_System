using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Stock;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockEntryRepository(AppDbContext dbContext) : IStockEntryRepository
{
    public async Task<StockEntry?> GetBySkuAndStoreAsync(
        Guid skuId, Guid storeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.StockEntries
            .FirstOrDefaultAsync(s => s.SkuId == skuId && s.StoreId == storeId, cancellationToken);
    }

    public async Task DeductStockAsync(
        Guid skuId, Guid storeId, decimal qty, CancellationToken cancellationToken = default)
    {
        // Dùng raw SQL để deduct atomic, tránh lost-update trong transaction serializable
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE stock_entries SET qty_on_hand = qty_on_hand - {qty}, last_updated = NOW() WHERE sku_id = {skuId} AND store_id = {storeId}""",
            cancellationToken);
    }
}
