namespace POS.Domain.Promotions.Services.Models;

public record PromotionCartItem(
    Guid SkuId,
    string SkuCode,
    Guid CategoryId,
    decimal Quantity,
    decimal UnitPrice)
{
    public decimal OriginalLineTotal => Quantity * UnitPrice;
}
