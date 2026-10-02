using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Commands.CreatePromotion;

public record CreatePromotionCommand(
    Guid? StoreId,
    string Name,
    string Type,
    decimal Value,
    decimal MinOrderAmount = 0,
    decimal? MaxDiscountAmount = null,
    string? ConditionsJson = null,
    int Priority = 0,
    bool IsStackable = false,
    bool IsExclusive = false,
    string AppliesTo = "All",
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null,
    IReadOnlyList<Guid>? TargetCategoryIds = null,
    IReadOnlyList<Guid>? TargetSkuIds = null,
    Guid? CreatedBy = null
) : IRequest<Result<PromotionDetailDto>>;
