using POS.Domain.Configuration;

namespace POS.Application.Abstractions.Persistence;

public interface ISystemConfigRepository
{
    Task<List<SystemConfig>> GetByStoreAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<SystemConfig?> GetAsync(Guid? storeId, string key, CancellationToken cancellationToken = default);
    Task AddAsync(SystemConfig config, CancellationToken cancellationToken = default);
    void Remove(SystemConfig config);
}
