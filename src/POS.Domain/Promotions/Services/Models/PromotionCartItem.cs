namespace POS.Domain.Promotions.Services.Models;

///  Represents a priced cart line used during promotion evaluation.</summary>
public record PromotionCartItem(
    Guid SkuId,
    string SkuCode,
    Guid CategoryId,
    decimal Quantity,
    decimal UnitPrice)
{
  public decimal OriginalLineTotal => Quantity * UnitPrice;
}
