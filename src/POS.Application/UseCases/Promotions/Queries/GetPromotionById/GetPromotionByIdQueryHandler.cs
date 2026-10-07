using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotionById;

public class GetPromotionByIdQueryHandler(IPromotionRepository promotionRepository)
    : IRequestHandler<GetPromotionByIdQuery, Result<PromotionDetailDto>>
{
    public async Task<Result<PromotionDetailDto>> Handle(GetPromotionByIdQuery request, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdWithTargetsAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.NotFound;
        }

        var targetCategoryIds = promotion.Targets
            .Where(t => t.CategoryId.HasValue)
            .Select(t => t.CategoryId!.Value)
            .ToList();

        var targetSkuIds = promotion.Targets
            .Where(t => t.SkuId.HasValue)
            .Select(t => t.SkuId!.Value)
            .ToList();

        var dto = new PromotionDetailDto(
            promotion.Id,
            promotion.StoreId,
            promotion.Name,
            promotion.Type.ToString(),
            promotion.Value,
            promotion.MinOrderAmount,
            promotion.MaxDiscountAmount,
            promotion.ConditionsJson,
            promotion.Priority,
            promotion.IsStackable,
            promotion.IsExclusive,
            promotion.AppliesTo.ToString(),
            new DateTimeOffset(DateTime.SpecifyKind(promotion.ValidFrom, DateTimeKind.Utc)),
            promotion.ValidTo.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(promotion.ValidTo.Value, DateTimeKind.Utc)) : null,
            promotion.Status.ToString(),
            targetCategoryIds,
            targetSkuIds,
            new DateTimeOffset(DateTime.SpecifyKind(promotion.CreatedAt, DateTimeKind.Utc))
        );

        return dto;
    }
}
