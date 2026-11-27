using FluentValidation;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Queries.GetApplicablePromotionsForSku;

public sealed record GetApplicablePromotionsForSkuQuery(Guid SkuId, decimal Quantity = 1)
    : IQuery<ApplicablePromotionsForSkuDto>, IRequirePermission
{
    public string RequiredPermission => "promotions:read";
}

public sealed class GetApplicablePromotionsForSkuQueryValidator : AbstractValidator<GetApplicablePromotionsForSkuQuery>
{
    public GetApplicablePromotionsForSkuQueryValidator()
    {
        RuleFor(query => query.SkuId).NotEmpty();
        RuleFor(query => query.Quantity).GreaterThan(0);
    }
}

public sealed class GetApplicablePromotionsForSkuQueryHandler(
    IStoreAccessPolicy storeAccess,
    ISkuRepository skus,
    IPromotionRepository promotions,
    PromotionDetailsReader detailsReader)
    : IQueryHandler<GetApplicablePromotionsForSkuQuery, ApplicablePromotionsForSkuDto>
{
    public async Task<Result<ApplicablePromotionsForSkuDto>> Handle(
        GetApplicablePromotionsForSkuQuery query, CancellationToken cancellationToken)
    {
        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return store.Error;

        var sku = await skus.GetByIdWithProductAsync(query.SkuId, cancellationToken);
        if (sku is null || sku.StoreId != store.Value)
            return OrderErrors.SkuNotFound;

        var categoryId = sku.Product?.CategoryId;
        var active = await promotions.GetActivePromotionsAsync(store.Value, DateTime.UtcNow, cancellationToken);
        var matches = active.Where(p => p.AppliesTo switch
        {
            PromotionAppliesTo.All => true,
            PromotionAppliesTo.SKU => p.Targets.Any(t => t.SkuId == sku.Id),
            PromotionAppliesTo.Category => categoryId.HasValue && p.Targets.Any(t => t.CategoryId == categoryId),
            _ => false
        }).ToList();

        var details = await detailsReader.ReadAsync(store.Value, matches, cancellationToken);
        return new ApplicablePromotionsForSkuDto(sku.Id, query.Quantity, details);
    }
}
