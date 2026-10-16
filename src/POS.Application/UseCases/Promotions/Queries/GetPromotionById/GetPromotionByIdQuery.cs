using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotionById;

public record GetPromotionByIdQuery(Guid Id) : IQuery<PromotionDetailDto>, IRequirePermission
{
    public string RequiredPermission => "promotions:read";
}
