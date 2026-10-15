using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.StockTake;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockTakeRepository(AppDbContext dbContext) : IStockTakeRepository
{
    public async Task<StockTake?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.StockTakes
            .Include(t => t.Items)
                .ThenInclude(i => i.Sku)
                    .ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<(List<StockTake> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(100, Math.Max(1, pageSize));

        var query = dbContext.StockTakes
            .AsNoTracking()
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(t => t.StoreId == storeId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StockTakeStatus>(status, true, out var parsedStatus))
            query = query.Where(t => t.Status == parsedStatus);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value.UtcDateTime);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value.UtcDateTime);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(StockTake stockTake, CancellationToken cancellationToken = default)
    {
        await dbContext.StockTakes.AddAsync(stockTake, cancellationToken);
    }
}
