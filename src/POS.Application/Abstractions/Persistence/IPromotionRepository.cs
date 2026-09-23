using POS.Domain.Promotions;

namespace POS.Application.Abstractions.Persistence;

public interface IPromotionRepository
{
    Task<IReadOnlyList<Promotion>> GetActiveAutomaticPromotionsAsync(Guid storeId, DateTime now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(Guid storeId, DateTime now, CancellationToken cancellationToken = default);
}
