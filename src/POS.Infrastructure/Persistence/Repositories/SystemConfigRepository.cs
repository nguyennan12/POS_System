using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Configuration;

namespace POS.Infrastructure.Persistence.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly AppDbContext _context;

    public SystemConfigRepository(AppDbContext context) => _context = context;

    public Task<List<SystemConfig>> GetByStoreAsync(Guid? storeId, CancellationToken cancellationToken = default) =>
        _context.SystemConfigs
            .AsNoTracking()
            .Where(c => c.StoreId == storeId)
            .ToListAsync(cancellationToken);

    public Task<SystemConfig?> GetAsync(Guid? storeId, string key, CancellationToken cancellationToken = default) =>
        _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.Key == key, cancellationToken);

    public async Task AddAsync(SystemConfig config, CancellationToken cancellationToken = default) =>
        await _context.SystemConfigs.AddAsync(config, cancellationToken);

    public void Remove(SystemConfig config) =>
        _context.SystemConfigs.Remove(config);
}
