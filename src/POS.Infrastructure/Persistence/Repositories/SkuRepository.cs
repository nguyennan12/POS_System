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
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sku?> GetByBarcodeWithProductAsync(string barcode, Guid storeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Skus
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(s => s.Barcode == barcode && s.StoreId == storeId, cancellationToken);
    }

    public async Task<IReadOnlyList<Sku>> GetByIdsWithProductAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await dbContext.Skus
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(cancellationToken);
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
}
