using POS.Domain.Promotions.Services.Models;

namespace POS.Domain.Promotions.Services;

public interface IPromotionEngine
{
    PromotionResult Evaluate(
        PromotionCart cart,
        IEnumerable<Promotion> activePromotions,
        DateTime now,
        Voucher? appliedVoucher = null);
}
