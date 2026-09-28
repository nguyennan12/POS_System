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
            .Include(e => e.Sku).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(s => s.SkuId == skuId && s.StoreId == storeId, cancellationToken);
    }

    public async Task<(List<StockEntry> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        Guid? skuId,
        Guid? categoryId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(100, Math.Max(1, pageSize));

        var query = dbContext.StockEntries
            .AsNoTracking()
            .Include(e => e.Sku).ThenInclude(s => s.Product)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(e => e.StoreId == storeId.Value);

        if (skuId.HasValue)
            query = query.Where(e => e.SkuId == skuId.Value);

        if (categoryId.HasValue)
            query = query.Where(e => e.Sku.Product.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Sku.SkuCode.ToLower().Contains(term) ||
                e.Sku.Product.Name.ToLower().Contains(term) ||
                (e.Sku.Barcode != null && e.Sku.Barcode.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.Sku.Product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<StockEntry> Items, int TotalCount)> GetAlertsAsync(
        Guid? storeId,
        int nearExpiryDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(100, Math.Max(1, pageSize));

        var expiryThreshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(nearExpiryDays));

        // Lấy SKU IDs của lô gần hết hạn
        var batchesQuery = dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.Qty > 0
                        && b.ExpiryDate.HasValue
                        && b.ExpiryDate.Value <= expiryThreshold);

        if (storeId.HasValue)
            batchesQuery = batchesQuery.Where(b => b.StoreId == storeId.Value);

        var nearExpirySkuIds = await batchesQuery
            .Select(b => b.SkuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = dbContext.StockEntries
            .AsNoTracking()
            .Include(e => e.Sku).ThenInclude(s => s.Product)
            .Where(e => (e.QtyOnHand <= e.MinStock || nearExpirySkuIds.Contains(e.SkuId)));

        if (storeId.HasValue)
            query = query.Where(e => e.StoreId == storeId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.Sku.Product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<StockBatch> Items, int TotalCount)> GetBatchesAsync(
        Guid? storeId,
        Guid? skuId,
        DateOnly? expiryBefore,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(100, Math.Max(1, pageSize));

        var query = dbContext.StockBatches
            .AsNoTracking()
            .Include(b => b.Sku).ThenInclude(s => s.Product)
            .Where(b => b.Qty > 0) // filter results to batches with quantity greater than zero
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(b => b.StoreId == storeId.Value);

        if (skuId.HasValue)
            query = query.Where(b => b.SkuId == skuId.Value);

        if (expiryBefore.HasValue)
            query = query.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= expiryBefore.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.ExpiryDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(StockEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.StockEntries.AddAsync(entry, cancellationToken);
    }

    public async Task DeductStockAsync(
        Guid skuId, Guid storeId, decimal qty, CancellationToken cancellationToken = default)
    {
        // Dùng raw SQL để deduct atomic, tránh lost-update trong transaction serializable
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE stock_entries SET qty_on_hand = qty_on_hand - {qty}, last_updated = NOW() WHERE sku_id = {skuId} AND store_id = {storeId}""",
            cancellationToken);
    }

    public async Task DeductStockWithBatchesAsync(
        Guid skuId, Guid storeId, decimal qty, CancellationToken cancellationToken = default)
    {
        // 1. Trừ tổng tồn kho atomic trong stock_entries
        await DeductStockAsync(skuId, storeId, qty, cancellationToken);

        // 2. Lấy danh sách các Lô còn tồn theo thứ tự ưu tiên FEFO (hạn dùng sớm nhất -> ngày nhập sớm nhất)
        var batches = await dbContext.StockBatches
            .Where(b => b.StoreId == storeId && b.SkuId == skuId && b.Qty > 0)
            .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
            .ThenBy(b => b.ExpiryDate)
            .ThenBy(b => b.ReceivedAt)
            .ToListAsync(cancellationToken);

        var remaining = qty;
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var deductAmount = Math.Min(batch.Qty, remaining);
            batch.DeductQty(deductAmount);
            remaining -= deductAmount;
        }
    }

    public async Task IncrementStockAsync(
        Guid skuId, Guid storeId, decimal qty, decimal newAverageCost, CancellationToken cancellationToken = default)
    {
        // Cộng tồn kho và cập nhật giá vốn bình quân atomic, tránh lost-update
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE stock_entries SET qty_on_hand = qty_on_hand + {qty}, average_cost = {newAverageCost}, last_updated = NOW() WHERE sku_id = {skuId} AND store_id = {storeId}""",
            cancellationToken);
    }
}

