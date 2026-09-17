namespace POS.Domain.Promotions.Services.Models;

/// <summary>Represents the cart context supplied to the promotion engine.</summary>
public record PromotionCart(
    Guid StoreId,
    IReadOnlyList<PromotionCartItem> Items,
    Guid? CustomerId = null,
    Guid? CustomerTierId = null,
    string? VoucherCode = null)
{
    public decimal Subtotal => Items.Sum(item => item.OriginalLineTotal);
}
