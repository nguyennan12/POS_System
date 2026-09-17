using POS.Domain.Promotions.Services.Models;

namespace POS.Domain.Promotions.Services;

public interface IPromotionEngine
{
    /// <summary>
    /// Evaluates eligible promotions for a cart and returns the resulting item and order discounts.
    /// </summary>
    PromotionResult Evaluate(
        PromotionCart cart,
        IEnumerable<Promotion> activePromotions,
        DateTime now,
        Voucher? appliedVoucher = null);
}
