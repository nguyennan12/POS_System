namespace POS.Domain.Promotions.Services.Models;

public record CartItemDiscountResult(
    Guid SkuId,
    decimal OriginalLineTotal,
    decimal DiscountAmount,
    decimal FinalLineTotal);
