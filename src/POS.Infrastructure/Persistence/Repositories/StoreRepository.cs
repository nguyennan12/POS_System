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

    public Task<List<Store>> GetAccessibleAsync(Guid employeeId, bool isChainOwner, Guid? storeId, CancellationToken cancellationToken = default) =>
        _context.Stores.AsNoTracking()
            .Where(s => isChainOwner
                ? _context.EmployeeStoreAccesses.Any(a => a.EmployeeId == employeeId && a.StoreId == s.Id)
                : s.Id == storeId)
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Store store, CancellationToken cancellationToken = default) =>
        await _context.Stores.AddAsync(store, cancellationToken);
}
