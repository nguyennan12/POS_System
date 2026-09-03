using POS.Domain.Stores;

namespace POS.Application.Abstractions.Persistence;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Store>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Store store, CancellationToken cancellationToken = default);
}
