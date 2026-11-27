using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Promotions;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services;
using POS.Domain.Promotions.Services.Models;

namespace POS.Application.UseCases.Orders.Services;

internal sealed class CartPromotionEvaluator(
    ISkuRepository skuRepository,
    IPromotionRepository promotionRepository,
    IVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    IPromotionEngine promotionEngine)
{
    internal Task<Result<CartPromotionEvaluation>> EvaluateOrderAsync(
        Order order, Voucher? voucher, bool revalidateVoucher, bool enforceStoreScope,
        CancellationToken cancellationToken) =>
        EvaluateAsync(
            order.StoreId,
            order.Items.Select(i => new CartLine(i.SkuId, i.Qty, i.UnitPrice)).ToList(),
            order.CustomerId, voucher, order.AppliedVoucherId, voucherCode: null,
            revalidateVoucher, enforceStoreScope, cancellationToken);

    internal Task<Result<CartPromotionEvaluation>> EvaluateCartAsync(
        Guid storeId, PromotionPreviewCart cart, CancellationToken cancellationToken) =>
        EvaluateAsync(
            storeId,
            cart.Items.GroupBy(i => i.SkuId)
                .Select(g => new CartLine(g.Key, g.Sum(i => i.Quantity), null)).ToList(),
            cart.CustomerId, voucher: null, voucherId: null, cart.VoucherCode,
            revalidateVoucher: true, enforceStoreScope: true, cancellationToken);

    private async Task<Result<CartPromotionEvaluation>> EvaluateAsync(
        Guid storeId, IReadOnlyList<CartLine> lines, Guid? customerId, Voucher? voucher,
        Guid? voucherId, string? voucherCode, bool revalidateVoucher, bool enforceStoreScope,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = new List<PromotionCartItem>(lines.Count);
        var skuTaxRates = new Dictionary<Guid, decimal>();
        if (lines.Count == 0)
        {
            var emptyCart = new PromotionCart(storeId, items);
            return new CartPromotionEvaluation(emptyCart, promotionEngine.Evaluate(emptyCart, [], now), skuTaxRates, [], null, now);
        }

        var skuIds = lines.Select(i => i.SkuId).Distinct();
        var skus = (await skuRepository.GetByIdsWithProductAsync(skuIds, cancellationToken)).ToDictionary(s => s.Id);
        Guid? customerTierId = null;
        if (customerId is Guid customer)
        {
            var customerWithPoints = await customerRepository.GetByIdAsync(customer, cancellationToken);
            customerTierId = customerWithPoints?.Customer.MemberTierId;
        }

        foreach (var line in lines)
        {
            if (!skus.TryGetValue(line.SkuId, out var sku) || (enforceStoreScope && sku.StoreId != storeId))
                return OrderErrors.SkuNotFound;

            if (!sku.IsActive)
                return OrderErrors.SkuInactive;

            items.Add(new PromotionCartItem(sku.Id, sku.SkuCode, sku.Product?.CategoryId ?? Guid.Empty,
                line.Quantity, line.UnitPrice ?? sku.SellPrice));
            skuTaxRates[sku.Id] = sku.TaxRate;
        }

        if (voucher is null && voucherId is Guid appliedVoucherId)
            voucher = await voucherRepository.GetByIdWithPromotionAsync(appliedVoucherId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            voucher = await voucherRepository.GetByCodeWithPromotionAsync(voucherCode.Trim(), cancellationToken);
            if (voucher is null)
                return OrderErrors.VoucherNotFound;
        }

        if (enforceStoreScope && voucher?.Promotion is { StoreId: var voucherStore } &&
            voucherStore != Guid.Empty && voucherStore != storeId)
            return OrderErrors.VoucherNotFound;

        if (revalidateVoucher && voucher is not null)
        {
            var error = await ValidateVoucherAsync(voucher, customerId, now, cancellationToken);
            if (error != Error.None)
                return error;
        }

        var cart = new PromotionCart(storeId, items, customerId, customerTierId, voucher?.Code);
        var automatic = await promotionRepository.GetActiveAutomaticPromotionsAsync(storeId, now, cancellationToken);
        var promotions = automatic.ToList();
        if (voucher?.Promotion is { } voucherPromotion && promotions.All(p => p.Id != voucherPromotion.Id))
            promotions.Add(voucherPromotion);

        var result = promotionEngine.Evaluate(cart, promotions, now, voucher);
        return new CartPromotionEvaluation(cart, result, skuTaxRates, promotions, voucher, now);
    }

    private async Task<Error> ValidateVoucherAsync(
        Voucher voucher, Guid? customerId, DateTime now, CancellationToken cancellationToken)
    {
        if (!voucher.IsActive)
            return OrderErrors.VoucherInactive;
        if (voucher.ExpiresAt.HasValue && now > voucher.ExpiresAt.Value)
            return OrderErrors.VoucherExpired;
        if (voucher.UsedCount >= voucher.MaxUses)
            return OrderErrors.VoucherUsageLimitReached;
        if (voucher.Promotion?.Status != PromotionStatus.Active)
            return OrderErrors.VoucherInactive;

        if (customerId is Guid customer)
        {
            var usage = await voucherRepository.GetCustomerUsageCountAsync(voucher.Id, customer, cancellationToken);
            if (usage >= voucher.PerCustomerLimit)
                return OrderErrors.VoucherCustomerLimitReached;
        }

        return Error.None;
    }

    private sealed record CartLine(Guid SkuId, decimal Quantity, decimal? UnitPrice);
}

internal sealed record CartPromotionEvaluation(
    PromotionCart Cart,
    PromotionResult Promotions,
    Dictionary<Guid, decimal> SkuTaxRates,
    IReadOnlyList<Promotion> Candidates,
    Voucher? Voucher,
    DateTime EvaluatedAt);
