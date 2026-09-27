using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class PromotionRepository(AppDbContext dbContext) : IPromotionRepository
{
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
}
