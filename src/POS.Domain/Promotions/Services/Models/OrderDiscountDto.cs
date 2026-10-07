namespace POS.Domain.Promotions.Services.Models;

///  Represents an order-level discount created by promotion evaluation.</summary>
public record OrderDiscountDto(
    Guid Id,
    Guid? PromotionId,
    Guid? VoucherId,
    decimal DiscountAmount,
    string? Description,
    DateTimeOffset AppliedAt);
