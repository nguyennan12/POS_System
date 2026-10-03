using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Commands.UpdatePromotion;

public class UpdatePromotionCommandHandler(
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePromotionCommand, Result<PromotionDetailDto>>
{
    public async Task<Result<PromotionDetailDto>> Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdWithTargetsAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.NotFound;
        }

        var type = Enum.Parse<PromotionType>(request.Type, true);
        var appliesTo = Enum.Parse<PromotionAppliesTo>(request.AppliesTo, true);
        var status = Enum.Parse<PromotionStatus>(request.Status, true);

        var effectiveValidFrom = request.ValidFrom?.UtcDateTime ?? promotion.ValidFrom;
        var effectiveValidTo = request.ValidTo?.UtcDateTime;

        if (effectiveValidTo.HasValue && effectiveValidTo.Value < effectiveValidFrom)
        {
            return PromotionErrors.InvalidDateRange;
        }

        promotion.Update(
            name: request.Name,
            type: type,
            value: request.Value,
            minOrderAmount: request.MinOrderAmount,
            maxDiscountAmount: request.MaxDiscountAmount,
            conditionsJson: request.ConditionsJson,
            priority: request.Priority,
            isStackable: request.IsStackable,
            isExclusive: request.IsExclusive,
            appliesTo: appliesTo,
            validFrom: effectiveValidFrom,
            validTo: effectiveValidTo,
            status: status
        );

        promotion.ClearTargets();

        if (appliesTo == PromotionAppliesTo.Category && request.TargetCategoryIds != null)
        {
            foreach (var categoryId in request.TargetCategoryIds.Distinct())
            {
                promotion.AddTargetCategory(categoryId);
            }
        }
        else if (appliesTo == PromotionAppliesTo.SKU && request.TargetSkuIds != null)
        {
            foreach (var skuId in request.TargetSkuIds.Distinct())
            {
                promotion.AddTargetSku(skuId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
