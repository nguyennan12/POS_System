using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;
using POS.Domain.Products.Enums;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public async Task<(List<ProductSummaryRow> Items, int TotalCount)> GetPagedAsync(
        Guid storeId,
        string? search,
        Guid? categoryId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking()
            .Where(p => p.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Brand != null && p.Brand.ToLower().Contains(s)));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<ProductStatus>(status, true, out var parsedStatus))
                query = query.Where(p => p.Status == parsedStatus);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummaryRow(p, dbContext.Skus.Count(s => s.ProductId == p.Id)))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Product?> GetByIdWithSkusAsync(Guid id, Guid storeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Skus)
            .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == storeId, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        dbContext.Products.Update(product);
    }

    public void Remove(Product product)
    {
        dbContext.Products.Remove(product);
    }
}
