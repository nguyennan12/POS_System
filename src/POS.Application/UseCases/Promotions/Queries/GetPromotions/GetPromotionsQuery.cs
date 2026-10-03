using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotions;

public record GetPromotionsQuery(
    Guid? StoreId = null,
    string? Status = null,
    string? Type = null,
    DateTimeOffset? ActiveAt = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedPromotionList>, IRequirePermission
{
    public string RequiredPermission => "discounts:read";
}
