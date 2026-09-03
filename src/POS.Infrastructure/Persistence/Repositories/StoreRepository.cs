using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<List<Store>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Stores.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Store store, CancellationToken cancellationToken = default) =>
        await _context.Stores.AddAsync(store, cancellationToken);
}
