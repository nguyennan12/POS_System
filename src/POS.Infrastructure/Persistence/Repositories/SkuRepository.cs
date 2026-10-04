using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;

namespace POS.Infrastructure.Persistence.Repositories;

public class SkuRepository(AppDbContext dbContext) : ISkuRepository
{
    public async Task<Sku?> GetByIdWithProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Skus
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .Include(s => s.UnitConversions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sku?> GetByBarcodeWithProductAsync(string barcode, Guid storeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Skus
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .Include(s => s.UnitConversions)
            .FirstOrDefaultAsync(s => s.Barcode == barcode && s.StoreId == storeId, cancellationToken);
    }

    public async Task<IReadOnlyList<Sku>> GetByIdsWithProductAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await dbContext.Skus
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .Include(s => s.UnitConversions)
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, Sku>> GetBySkuCodesAsync(
        IEnumerable<string> skuCodes,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var codes = skuCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (codes.Count == 0) return new Dictionary<string, Sku>(StringComparer.OrdinalIgnoreCase);

        var skus = await dbContext.Skus
            .Where(s => s.StoreId == storeId && codes.Contains(s.SkuCode))
            .ToListAsync(cancellationToken);

        return skus.ToDictionary(s => s.SkuCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task<HashSet<string>> GetExistingBarcodesAsync(
        IEnumerable<string> barcodes,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var bcList = barcodes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (bcList.Count == 0) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var existing = await dbContext.Skus
            .Where(s => s.StoreId == storeId && bcList.Contains(s.Barcode))
            .Select(s => s.Barcode)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsSkuCodeUniqueAsync(string skuCode, Guid storeId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return !await dbContext.Skus.AnyAsync(s => s.StoreId == storeId && s.SkuCode == skuCode && (!excludeId.HasValue || s.Id != excludeId.Value), cancellationToken);
    }

    public async Task<bool> IsBarcodeUniqueAsync(string barcode, Guid storeId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return !await dbContext.Skus.AnyAsync(s => s.StoreId == storeId && s.Barcode == barcode && (!excludeId.HasValue || s.Id != excludeId.Value), cancellationToken);
    }

    public async Task AddAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        await dbContext.Skus.AddAsync(sku, cancellationToken);
    }

    public void Remove(Sku sku)
    {
        dbContext.Skus.Remove(sku);
    }
}
