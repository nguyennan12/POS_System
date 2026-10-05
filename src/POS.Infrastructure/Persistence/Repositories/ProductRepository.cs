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

        // Use a SQL-level anonymous projection that includes Category.Name directly,
        // avoiding the EF Core behaviour where .Select() silently discards .Include() navigations.
        var rawItems = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.StoreId,
                p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                p.Name,
                p.Description,
                p.Brand,
                p.BaseUnit,
                p.ImageUrl,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt,
                SkuCount = dbContext.Skus.Count(s => s.ProductId == p.Id)
            })
            .ToListAsync(cancellationToken);

        // Attach a tracked Product entity so callers that need navigation still work.
        // For paged list we only need scalars — so attach AsNoTracking entities for the Product wrapper.
        var items = rawItems
            .Select(r =>
            {
                var product = dbContext.Products.Local.FirstOrDefault(p => p.Id == r.Id)
                    ?? new ProductPlaceholder(r.Id, r.StoreId, r.CategoryId, r.Name, r.BaseUnit,
                        r.Description, r.Brand, r.ImageUrl, r.Status, r.CreatedAt, r.UpdatedAt);
                return new ProductSummaryRow(product, r.SkuCount, r.CategoryName);
            })
            .ToList();

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

/// <summary>
/// In-memory Product placeholder used when projecting paged results.
/// Carries scalar values from the SQL projection without EF navigation tracking.
/// </summary>
internal sealed class ProductPlaceholder : Product
{
    public ProductPlaceholder(
        Guid id, Guid storeId, Guid categoryId, string name, string baseUnit,
        string? description, string? brand, string? imageUrl,
        ProductStatus status, DateTime createdAt, DateTime updatedAt)
        : base(storeId, categoryId, name, baseUnit, description, brand, imageUrl, status, id)
    {
    }
}
