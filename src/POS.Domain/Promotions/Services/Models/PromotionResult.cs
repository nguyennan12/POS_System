namespace POS.Domain.Promotions.Services.Models;

///  Contains cart totals and discount details produced by promotion evaluation.</summary>
public record PromotionResult(
    decimal Subtotal,
    decimal TotalDiscount,
    decimal GrandTotal,
    IReadOnlyList<AppliedPromotionResult> AppliedPromotions,
    IReadOnlyList<CartItemDiscountResult> ItemDiscounts,
    IReadOnlyList<OrderDiscountDto> OrderDiscounts);
