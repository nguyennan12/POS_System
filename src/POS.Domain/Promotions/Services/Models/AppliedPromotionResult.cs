using POS.Domain.Promotions.Enums;

namespace POS.Domain.Promotions.Services.Models;

public record AppliedPromotionResult(
    Guid PromotionId,
    string PromotionName,
    PromotionType Type,
    decimal DiscountAmount,
    string? Description = null,
    Guid? VoucherId = null);
