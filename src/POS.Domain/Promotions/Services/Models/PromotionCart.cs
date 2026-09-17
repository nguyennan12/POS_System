namespace POS.Domain.Promotions.Services.Models;

public record PromotionCart(
    Guid StoreId,
    IReadOnlyList<PromotionCartItem> Items,
    Guid? CustomerId = null,
    Guid? CustomerTierId = null,
    string? VoucherCode = null)
{
    public decimal Subtotal => Items.Sum(item => item.OriginalLineTotal);
}
