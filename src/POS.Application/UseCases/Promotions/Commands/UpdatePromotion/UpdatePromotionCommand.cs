using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Commands.UpdatePromotion;

public record UpdatePromotionCommand(
    Guid Id,
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
    string Status = "Active",
    IReadOnlyList<Guid>? TargetCategoryIds = null,
    IReadOnlyList<Guid>? TargetSkuIds = null
) : ICommand<PromotionDetailDto>, IRequirePermission
{
    public string RequiredPermission => "discounts:update";
}
