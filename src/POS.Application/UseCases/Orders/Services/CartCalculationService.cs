using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Services;
using POS.Domain.Promotions.Services.Models;

namespace POS.Application.UseCases.Orders.Services;

public class CartCalculationService(
    ISkuRepository skuRepository,
    IPromotionRepository promotionRepository,
    IVoucherRepository voucherRepository,
    ICustomerRepository customerRepository,
    IPromotionEngine promotionEngine) : ICartCalculationService
{
    public async Task<Result> RecalculateAsync(
        Order order,
        Voucher? appliedVoucher = null,
        CancellationToken cancellationToken = default)
    {
        if (order.Items.Count == 0)
        {
            order.ClearItems();
            return Result.Success();
        }

        var now = DateTime.UtcNow;

        // 1. Lấy thông tin các SKU trong giỏ hàng
        var skuIds = order.Items.Select(i => i.SkuId).Distinct().ToList();
        var skus = await skuRepository.GetByIdsWithProductAsync(skuIds, cancellationToken);
        var skuDict = skus.ToDictionary(s => s.Id);

        // 2. Lấy thông tin khách hàng nếu có
        Guid? customerTierId = null;
        if (order.CustomerId.HasValue)
        {
            var customerWithPoints = await customerRepository.GetByIdAsync(order.CustomerId.Value, cancellationToken);
            customerTierId = customerWithPoints?.Customer.MemberTierId;
        }

        // 3. Chuẩn bị PromotionCart và kiểm tra tính toàn vẹn của SKU
        var cartItems = new List<PromotionCartItem>();
        var skuTaxRates = new Dictionary<Guid, decimal>();

        foreach (var item in order.Items)
        {
            if (!skuDict.TryGetValue(item.SkuId, out var sku))
            {
                return OrderErrors.SkuNotFound;
            }

            if (!sku.IsActive)
            {
                return OrderErrors.SkuInactive;
            }

            cartItems.Add(new PromotionCartItem(
                SkuId: sku.Id,
                SkuCode: sku.SkuCode,
                CategoryId: sku.Product?.CategoryId ?? Guid.Empty,
                Quantity: item.Qty,
                UnitPrice: item.UnitPrice
            ));

            skuTaxRates[sku.Id] = sku.TaxRate;
        }

        // 4. Khôi phục voucher từ Order nếu không được truyền trực tiếp
        if (appliedVoucher == null && order.AppliedVoucherId.HasValue)
        {
            appliedVoucher = await voucherRepository.GetByIdWithPromotionAsync(order.AppliedVoucherId.Value, cancellationToken);
        }

        var promoCart = new PromotionCart(
            StoreId: order.StoreId,
            Items: cartItems,
            CustomerId: order.CustomerId,
            CustomerTierId: customerTierId,
            VoucherCode: appliedVoucher?.Code
        );

        // 5. Lấy danh sách khuyến mãi tự động (loại bỏ khuyến mãi voucher)
        var automaticPromotions = await promotionRepository.GetActiveAutomaticPromotionsAsync(order.StoreId, now, cancellationToken);
        var promotionsToEvaluate = automaticPromotions.ToList();

        // Nếu có voucher được áp dụng, nạp promotion của voucher đó vào evaluation
        if (appliedVoucher?.Promotion != null && !promotionsToEvaluate.Any(p => p.Id == appliedVoucher.Promotion.Id))
        {
            promotionsToEvaluate.Add(appliedVoucher.Promotion);
        }

        // 6. Chạy PromotionEngine
        var promoResult = promotionEngine.Evaluate(promoCart, promotionsToEvaluate, now, appliedVoucher);

        // 7. Áp dụng kết quả vào Order và duy trì voucher state
        order.ApplyPromotionEvaluation(promoResult, skuTaxRates);
        if (appliedVoucher != null)
        {
            order.SetAppliedVoucher(appliedVoucher.Id, appliedVoucher.Code);
        }

        return Result.Success();
    }
}
