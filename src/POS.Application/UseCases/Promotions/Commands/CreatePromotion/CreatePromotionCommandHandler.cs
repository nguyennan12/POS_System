using MediatR;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Commands.CreatePromotion;

public class CreatePromotionCommandHandler(
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<CreatePromotionCommand, Result<PromotionDetailDto>>
{
    public async Task<Result<PromotionDetailDto>> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var type = Enum.Parse<PromotionType>(request.Type, true);
        var appliesTo = Enum.Parse<PromotionAppliesTo>(request.AppliesTo, true);

        var storeId = request.StoreId ?? (currentUser.IsChainOwner ? Guid.Empty : (currentUser.StoreId ?? Guid.Empty));
        var createdBy = request.CreatedBy ?? currentUser.EmployeeId ?? Guid.Empty;

        var promotion = new Promotion(
            storeId: storeId,
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
            validFrom: request.ValidFrom?.UtcDateTime,
            validTo: request.ValidTo?.UtcDateTime,
            status: PromotionStatus.Active,
            createdBy: createdBy
        );

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

        await promotionRepository.AddAsync(promotion, cancellationToken);
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
