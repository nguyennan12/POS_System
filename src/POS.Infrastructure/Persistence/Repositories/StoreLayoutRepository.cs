using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.StoreLayouts;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class StoreLayoutRepository(AppDbContext dbContext) : IStoreLayoutRepository
{
    public async Task<StoreLayout?> GetByIdAsync(
        Guid id,
        bool includeGraph = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StoreLayouts.AsQueryable();

        if (includeGraph)
        {
            query = query
                .Include(l => l.Zones)
                .Include(l => l.Cells)
                .Include(l => l.Shelves)
                    .ThenInclude(s => s.Levels)
                        .ThenInclude(lv => lv.Placements);
        }

        return await query.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<StoreLayout?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StoreLayouts
            .Include(l => l.Zones)
            .Include(l => l.Cells)
            .Include(l => l.Shelves)
                .ThenInclude(s => s.Levels)
                    .ThenInclude(lv => lv.Placements)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<StoreLayout?> GetActiveByStoreAsync(
        Guid storeId,
        bool includeGraph = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StoreLayouts.Where(l => l.StoreId == storeId && l.Status == StoreLayoutStatus.Active);

        if (includeGraph)
        {
            query = query
                .Include(l => l.Zones)
                .Include(l => l.Cells)
                .Include(l => l.Shelves)
                    .ThenInclude(s => s.Levels)
                        .ThenInclude(lv => lv.Placements);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<StoreLayoutSummaryDto> Items, int Total)> ListAsync(
        Guid storeId,
        StoreLayoutStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StoreLayouts
            .AsNoTracking()
            .Where(l => l.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.VersionNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new StoreLayoutSummaryDto(
                l.Id,
                l.StoreId,
                l.VersionNumber,
                l.Name,
                l.GridWidth,
                l.GridHeight,
                l.Status,
                l.Revision,
                l.CreatedAt,
                l.PublishedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<int> GetNextVersionNumberAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var maxVersion = await dbContext.StoreLayouts
            .Where(l => l.StoreId == storeId)
            .MaxAsync(l => (int?)l.VersionNumber, cancellationToken);

        return (maxVersion ?? 0) + 1;
    }

    public async Task<bool> HasActiveLayoutAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StoreLayouts
            .AnyAsync(l => l.StoreId == storeId && l.Status == StoreLayoutStatus.Active, cancellationToken);
    }

    public void Add(StoreLayout layout) => dbContext.StoreLayouts.Add(layout);

    public void Remove(StoreLayout layout) => dbContext.StoreLayouts.Remove(layout);
}
