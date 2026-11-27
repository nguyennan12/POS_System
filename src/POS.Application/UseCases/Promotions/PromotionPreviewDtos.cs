using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services.Models;

namespace POS.Application.UseCases.Promotions;

public sealed record PromotionTargetDto(Guid Id, string Name);

public sealed record ActivePromotionDetailDto(
    Guid PromotionId,
    string Name,
    PromotionType Type,
    decimal Value,
    PromotionAppliesTo AppliesTo,
    IReadOnlyList<PromotionTargetDto> TargetCategories,
    IReadOnlyList<PromotionTargetDto> TargetSkus,
    DateTime ValidFrom,
    DateTime? ValidTo,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    int Priority,
    bool IsStackable,
    bool IsExclusive,
    BuyXGetYCondition? BuyXGetY,
    string? FreeSkuName,
    HappyHourCondition? HappyHour,
    string DescriptionVi);

public sealed record ApplicablePromotionsForSkuDto(
    Guid SkuId,
    decimal Quantity,
    IReadOnlyList<ActivePromotionDetailDto> Promotions)
{
    public string Eligibility => "potential";
}

public sealed record PromotionPreviewCartItem(Guid SkuId, decimal Quantity);

public sealed record PromotionPreviewCart(
    IReadOnlyList<PromotionPreviewCartItem> Items,
    Guid? CustomerId,
    string? VoucherCode);

public sealed record PromotionPreviewSuggestion(
    Guid PromotionId,
    string PromotionName,
    IReadOnlyList<Guid> QualifyingSkuIds,
    decimal MissingQuantity,
    Guid? FreeSkuId,
    decimal MissingGiftQuantity,
    string DescriptionVi);

public sealed record PromotionPreviewResult(
    decimal Subtotal,
    decimal TotalDiscount,
    decimal TaxTotal,
    decimal GrandTotal,
    IReadOnlyList<AppliedPromotionResult> AppliedPromotions,
    IReadOnlyList<PromotionPreviewSuggestion> Suggestions);

public sealed record VoucherPreviewResult(
    bool IsValid,
    string? Reason,
    string? PromotionName,
    decimal DiscountAmount,
    decimal? GrandTotal);
