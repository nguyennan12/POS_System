using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Category>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Remove(Category category);
}
