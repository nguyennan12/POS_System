using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;

namespace POS.Application.Abstractions.Persistence;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Promotion?> GetByIdWithTargetsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetActiveAutomaticPromotionsAsync(Guid storeId, DateTime now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(Guid storeId, DateTime now, CancellationToken cancellationToken = default);
    Task<(List<Promotion> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        PromotionStatus? status,
        PromotionType? type,
        DateTimeOffset? activeAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    void Remove(Promotion promotion);
}
