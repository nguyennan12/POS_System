using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class PromotionRepository(AppDbContext dbContext) : IPromotionRepository
{
    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Promotions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Promotion?> GetByIdWithTargetsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Promotions
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAutomaticPromotionsAsync(
        Guid storeId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Promotions
            .Include(p => p.Targets)
            .Where(p => p.Status == PromotionStatus.Active
                && p.ValidFrom <= now
                && (!p.ValidTo.HasValue || p.ValidTo.Value >= now)
                && (p.StoreId == Guid.Empty || p.StoreId == storeId)
                && !dbContext.Vouchers.Any(v => v.PromotionId == p.Id && v.IsActive))
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(
        Guid storeId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Promotions
            .Include(p => p.Targets)
            .Where(p => p.Status == PromotionStatus.Active
                && p.ValidFrom <= now
                && (!p.ValidTo.HasValue || p.ValidTo.Value >= now)
                && (p.StoreId == Guid.Empty || p.StoreId == storeId))
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Promotion> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        PromotionStatus? status,
        PromotionType? type,
        DateTimeOffset? activeAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Promotions.AsNoTracking().Include(p => p.Targets).AsQueryable();

        if (storeId.HasValue && storeId.Value != Guid.Empty)
        {
            query = query.Where(p => p.StoreId == Guid.Empty || p.StoreId == storeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        if (activeAt.HasValue)
        {
            var activeUtc = activeAt.Value.UtcDateTime;
            query = query.Where(p => p.ValidFrom <= activeUtc && (!p.ValidTo.HasValue || p.ValidTo.Value >= activeUtc));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pNum = pageNumber < 1 ? 1 : pageNumber;
        var pSize = pageSize < 1 ? 20 : pageSize;

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await dbContext.Promotions.AddAsync(promotion, cancellationToken);
    }

    public void Remove(Promotion promotion)
    {
        dbContext.Promotions.Remove(promotion);
    }
}
