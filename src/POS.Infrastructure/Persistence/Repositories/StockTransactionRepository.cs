using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Stock;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockTransactionRepository(AppDbContext dbContext) : IStockTransactionRepository
{
    public async Task AddAsync(StockTransaction transaction, CancellationToken cancellationToken = default)
    {
        await dbContext.StockTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<(List<StockTransaction> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        Guid? skuId,
        string? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(100, Math.Max(1, pageSize));

        var query = dbContext.StockTransactions
            .AsNoTracking()
            .Include(t => t.Sku).ThenInclude(s => s.Product)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(t => t.StoreId == storeId.Value);

        if (skuId.HasValue)
            query = query.Where(t => t.SkuId == skuId.Value);

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<StockTransactionType>(type, true, out var parsedType))
            query = query.Where(t => t.Type == parsedType);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

