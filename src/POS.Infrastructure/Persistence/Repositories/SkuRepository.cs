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

    public async Task<(List<POS.Application.UseCases.Products.PosCatalogDto> Items, int TotalCount)> GetPosCatalogAsync(
        Guid storeId,
        Guid? categoryId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = from sku in dbContext.Skus.AsNoTracking()
                    join product in dbContext.Products.AsNoTracking() on sku.ProductId equals product.Id
                    join category in dbContext.Categories.AsNoTracking() on product.CategoryId equals category.Id into catGroup
                    from category in catGroup.DefaultIfEmpty()
                    join stock in dbContext.StockEntries.AsNoTracking().Where(s => s.StoreId == storeId) on sku.Id equals stock.SkuId into stockGroup
                    from stock in stockGroup.DefaultIfEmpty()
                    where sku.StoreId == storeId && sku.IsActive && product.Status == POS.Domain.Products.Enums.ProductStatus.Active
                    select new
                    {
                        sku.Id,
                        sku.ProductId,
                        product.Name,
                        sku.SkuCode,
                        sku.Barcode,
                        sku.SellPrice,
                        sku.CostPrice,
                        sku.TaxRate,
                        QtyOnHand = stock != null ? stock.QtyOnHand : 0m,
                        product.BaseUnit,
                        product.CategoryId,
                        CategoryName = category != null ? category.Name : string.Empty,
                        product.ImageUrl,
                        product.Brand
                    };

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s) ||
                                     x.SkuCode.ToLower().Contains(s) ||
                                     x.Barcode.ToLower().Contains(s) ||
                                     (x.Brand != null && x.Brand.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.SkuCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new POS.Application.UseCases.Products.PosCatalogDto(
            r.Id,
            r.ProductId,
            r.Name,
            r.SkuCode,
            r.Barcode,
            r.SellPrice,
            r.CostPrice,
            r.TaxRate,
            r.QtyOnHand,
            r.BaseUnit,
            r.CategoryId,
            r.CategoryName,
            r.ImageUrl
        )).ToList();

        return (items, total);
    }
}
