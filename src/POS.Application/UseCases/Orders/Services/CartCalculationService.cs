using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Services;

namespace POS.Application.UseCases.Orders.Services;

public class CartCalculationService(
    ISkuRepository skuRepository,
    IPromotionRepository promotionRepository,
    IVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    IPromotionEngine promotionEngine) : ICartCalculationService
{
    private readonly CartPromotionEvaluator evaluator = new(
        skuRepository, promotionRepository, voucherRepository, customerRepository, promotionEngine);

    public async Task<Result> RecalculateAsync(
        Order order,
        Voucher? appliedVoucher = null,
        CancellationToken cancellationToken = default,
        bool revalidateVoucher = false)
    {
        if (order.Items.Count == 0)
        {
            order.ClearItems();
            return Result.Success();
        }

        var evaluation = await evaluator.EvaluateOrderAsync(
            order, appliedVoucher, revalidateVoucher, enforceStoreScope: false, cancellationToken);
        if (evaluation.IsFailure)
            return evaluation.Error;

        var result = evaluation.Value!;
        order.ApplyPromotionEvaluation(result.Promotions, result.SkuTaxRates);
        if (result.Voucher is { } voucher)
            order.SetAppliedVoucher(voucher.Id, voucher.Code);

        return Result.Success();
    }
}
