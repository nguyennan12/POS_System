using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;

namespace POS.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<List<Category>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default) =>
        _context.Categories.Where(c => c.StoreId == storeId).AsNoTracking().ToListAsync(cancellationToken);

    public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Categories.AnyAsync(c => c.ParentId == id, cancellationToken);

    public Task<bool> HasProductsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Products.AnyAsync(p => p.CategoryId == id, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await _context.Categories.AddAsync(category, cancellationToken);

    public void Update(Category category) =>
        _context.Categories.Update(category);

    public void Remove(Category category) =>
        _context.Categories.Remove(category);
}
