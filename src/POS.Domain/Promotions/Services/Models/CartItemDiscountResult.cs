namespace POS.Domain.Promotions.Services.Models;

/// <summary>Summarizes the discount and final total for one cart item.</summary>
public record CartItemDiscountResult(
    Guid SkuId,
    decimal OriginalLineTotal,
    decimal DiscountAmount,
    decimal FinalLineTotal);
