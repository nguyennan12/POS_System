namespace POS.Domain.Promotions.Services.Models;

public record OrderDiscountDto(
    Guid Id,
    Guid? PromotionId,
    Guid? VoucherId,
    decimal DiscountAmount,
    string? Description,
    DateTimeOffset AppliedAt);
