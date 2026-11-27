using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Services;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services;
using POS.Domain.Promotions.Services.Models;

namespace POS.Application.UseCases.Promotions.Services;

public sealed class PromotionPreviewService(
    ICurrentUser currentUser,
    IStoreAccessPolicy storeAccess,
    IOrderRepository orders,
    ISkuRepository skus,
    IPromotionRepository promotions,
    IVoucherRepository vouchers,
    ICustomerRepository customers,
    IPromotionEngine promotionEngine,
    ILogger<PromotionPreviewService> logger) : IPromotionPreviewService
{
    private readonly CartPromotionEvaluator evaluator = new(skus, promotions, vouchers, customers, promotionEngine);

    public async Task<Result<PromotionPreviewResult>> PreviewOrderAsync(
        Guid orderId, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return OrderErrors.Unauthorized;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return store.Error;

        var order = await orders.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null || order.StoreId != store.Value)
            return OrderErrors.OrderNotFound;

        var evaluation = await evaluator.EvaluateOrderAsync(
            order, voucher: null, revalidateVoucher: true, enforceStoreScope: true, cancellationToken);
        return await BuildResultAsync(evaluation, cancellationToken);
    }

    public async Task<Result<PromotionPreviewResult>> PreviewCartAsync(
        PromotionPreviewCart cart, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return OrderErrors.Unauthorized;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return store.Error;

        if (cart.Items.Any(i => i.SkuId == Guid.Empty || i.Quantity <= 0))
            return new Error(ErrorType.Validation, "PROMOTION.INVALID_CART_ITEM", "Sản phẩm và số lượng trong giỏ hàng không hợp lệ.");

        var evaluation = await evaluator.EvaluateCartAsync(store.Value, cart, cancellationToken);
        return await BuildResultAsync(evaluation, cancellationToken);
    }

    private async Task<Result<PromotionPreviewResult>> BuildResultAsync(
        Result<CartPromotionEvaluation> evaluation, CancellationToken cancellationToken)
    {
        if (evaluation.IsFailure)
            return evaluation.Error;

        var result = evaluation.Value!;
        var discounts = result.Promotions.ItemDiscounts.ToDictionary(item => item.SkuId, item => item.DiscountAmount);
        var taxes = result.Cart.Items.Sum(item => OrderItem.CalculateDiscountAndTax(
            item.OriginalLineTotal, discounts.GetValueOrDefault(item.SkuId), result.SkuTaxRates[item.SkuId]).Tax);
        var totals = Order.CalculateTotals(result.Cart.Subtotal, result.Promotions.TotalDiscount, taxes);
        var suggestions = await GetSuggestionsAsync(result, cancellationToken);
        return new PromotionPreviewResult(totals.Subtotal, totals.Discount, totals.Tax, totals.GrandTotal,
            result.Promotions.AppliedPromotions, suggestions);
    }

    private async Task<IReadOnlyList<PromotionPreviewSuggestion>> GetSuggestionsAsync(
        CartPromotionEvaluation evaluation, CancellationToken cancellationToken)
    {
        var suggestions = new List<PromotionPreviewSuggestion>();
        var cart = evaluation.Cart;
        foreach (var promotion in evaluation.Candidates.Where(p =>
                     p.Type == PromotionType.BuyXGetY && p.IsActiveAt(evaluation.EvaluatedAt)))
        {
            var condition = PromotionConditions.Read<BuyXGetYCondition>(promotion, logger) ?? new BuyXGetYCondition();
            var qualifying = cart.Items.Where(i => MatchesTarget(promotion, i)).ToList();
            if (qualifying.Count == 0)
                continue;

            var buyQuantity = Math.Max(1, condition.BuyQuantity);
            var getQuantity = Math.Max(1, condition.GetQuantity);
            if (condition.FreeSkuId is Guid giftId)
            {
                var quantity = qualifying.Sum(i => i.Quantity);
                var missingPurchase = Math.Max(0, buyQuantity - quantity);
                var missingGift = missingPurchase > 0 ? 0 : Math.Max(0,
                    decimal.Floor(quantity / buyQuantity) * getQuantity -
                    (cart.Items.FirstOrDefault(i => i.SkuId == giftId)?.Quantity ?? 0));
                AddSuggestion(promotion, qualifying.Select(i => i.SkuId).ToList(), missingPurchase, giftId, missingGift);
            }
            else
            {
                foreach (var item in qualifying.Where(i => i.Quantity < (decimal)buyQuantity + getQuantity))
                {
                    var missingPurchase = Math.Max(0, buyQuantity - item.Quantity);
                    var missingGift = missingPurchase > 0 ? 0 : buyQuantity + (decimal)getQuantity - item.Quantity;
                    AddSuggestion(promotion, [item.SkuId], missingPurchase, item.SkuId, missingGift);
                }
            }
        }

        var giftIds = suggestions.Select(s => s.FreeSkuId).OfType<Guid>()
            .Except(cart.Items.Select(i => i.SkuId)).Distinct().ToList();
        if (giftIds.Count == 0)
            return suggestions;

        var storeGiftIds = (await skus.GetByIdsWithProductAsync(giftIds, cancellationToken))
            .Where(s => s.StoreId == cart.StoreId && s.IsActive).Select(s => s.Id).ToHashSet();
        storeGiftIds.UnionWith(cart.Items.Select(i => i.SkuId));
        return suggestions.Where(s => s.FreeSkuId is Guid id && storeGiftIds.Contains(id)).ToList();

        void AddSuggestion(Promotion promotion, IReadOnlyList<Guid> qualifyingSkuIds,
            decimal missingPurchase, Guid giftSkuId, decimal missingGift)
        {
            if (missingPurchase <= 0 && missingGift <= 0)
                return;

            var description = missingPurchase > 0
                ? $"Mua thêm {missingPurchase} sản phẩm đủ điều kiện cho khuyến mãi {promotion.Name}."
                : $"Thêm {missingGift} sản phẩm quà tặng vào giỏ cho khuyến mãi {promotion.Name}.";
            suggestions.Add(new PromotionPreviewSuggestion(
                promotion.Id, promotion.Name, qualifyingSkuIds, missingPurchase, giftSkuId, missingGift, description));
        }
    }

    private static bool MatchesTarget(Promotion promotion, PromotionCartItem item) => promotion.AppliesTo switch
    {
        PromotionAppliesTo.All => true,
        PromotionAppliesTo.SKU => promotion.Targets.Any(t => t.SkuId == item.SkuId),
        PromotionAppliesTo.Category => promotion.Targets.Any(t => t.CategoryId == item.CategoryId),
        _ => false
    };
}
