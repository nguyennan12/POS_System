namespace POS.Domain.Promotions.Services.Models;

public record PromotionResult(
    decimal Subtotal,
    decimal TotalDiscount,
    decimal GrandTotal,
    IReadOnlyList<AppliedPromotionResult> AppliedPromotions,
    IReadOnlyList<CartItemDiscountResult> ItemDiscounts,
    IReadOnlyList<OrderDiscountDto> OrderDiscounts);
