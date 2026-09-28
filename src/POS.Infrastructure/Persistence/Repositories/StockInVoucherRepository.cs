using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.StockIn;

namespace POS.Infrastructure.Persistence.Repositories;

public class StockInVoucherRepository(AppDbContext dbContext) : IStockInVoucherRepository
{
    public async Task<StockInVoucher?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.StockInVouchers
            .Include(v => v.Supplier)
            .Include(v => v.Items)
                .ThenInclude(i => i.Sku)
                    .ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<(List<StockInVoucher> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        Guid? supplierId,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(100, Math.Max(1, pageSize));

        var query = dbContext.StockInVouchers
            .AsNoTracking()
            .Include(v => v.Supplier)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(v => v.StoreId == storeId.Value);

        if (supplierId.HasValue)
            query = query.Where(v => v.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StockInVoucherStatus>(status, true, out var parsedStatus))
            query = query.Where(v => v.Status == parsedStatus);

        if (from.HasValue)
            query = query.Where(v => v.CreatedAt >= from.Value.UtcDateTime);

        if (to.HasValue)
            query = query.Where(v => v.CreatedAt <= to.Value.UtcDateTime);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(StockInVoucher voucher, CancellationToken cancellationToken = default)
    {
        await dbContext.StockInVouchers.AddAsync(voucher, cancellationToken);
    }
}
