using POS.Domain.Promotions.Enums;

namespace POS.Domain.Promotions.Services.Models;

/// <summary>Describes a promotion that was applied and the discount it produced.</summary>
public record AppliedPromotionResult(
    Guid PromotionId,
    string PromotionName,
    PromotionType Type,
    decimal DiscountAmount,
    string? Description = null,
    Guid? VoucherId = null);
