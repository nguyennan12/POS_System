using System.Text.Json;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services.Models;

namespace POS.Domain.Promotions.Services;

public class PromotionEngine : IPromotionEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public PromotionResult Evaluate(
        PromotionCart cart,
        IEnumerable<Promotion> activePromotions,
        DateTime now,
        Voucher? appliedVoucher = null)
    {
        if (cart.Items == null || cart.Items.Count == 0 || cart.Subtotal <= 0)
        {
            return new PromotionResult(
                Subtotal: 0,
                TotalDiscount: 0,
                GrandTotal: 0,
                AppliedPromotions: [],
                ItemDiscounts: [],
                OrderDiscounts: []);
        }

        // 1. Chuẩn bị trạng thái làm việc cho các item trong giỏ hàng
        var workingItems = cart.Items.Select(i => new WorkingCartItem(
            SkuId: i.SkuId,
            SkuCode: i.SkuCode,
            CategoryId: i.CategoryId,
            Quantity: i.Quantity,
            UnitPrice: i.UnitPrice,
            OriginalLineTotal: i.OriginalLineTotal
        )).ToList();

        // 2. Lọc các khuyến mãi thỏa điều kiện tiên quyết (Status, Time, Store, MinOrderAmount, HappyHour)
        var eligiblePromotions = activePromotions
            .Where(p => IsEligible(p, cart, now, appliedVoucher))
            .OrderByDescending(p => p.Priority)
            .ToList();

        var appliedPromotions = new List<AppliedPromotionResult>();
        var orderDiscounts = new List<OrderDiscountDto>();

        bool hasExclusiveApplied = false;
        bool hasNonStackableLineApplied = false;
        bool hasNonStackableCartApplied = false;

        // Phân tách khuyến mãi dòng hàng (Line-level) và khuyến mãi toàn đơn (Cart-level)
        // Quy tắc: Khuyến mãi theo dòng hàng được tính trước khuyến mãi toàn đơn
        var linePromotions = eligiblePromotions.Where(IsLineLevelPromotion).ToList();
        var cartPromotions = eligiblePromotions.Where(p => !IsLineLevelPromotion(p)).ToList();

        // 3. Đánh giá khuyến mãi theo dòng hàng (Line-level)
        foreach (var promo in linePromotions)
        {
            if (hasExclusiveApplied)
                break;

            if (promo.IsExclusive && appliedPromotions.Count > 0)
                continue; // Exclusive không thể đi cùng KM khác đã áp dụng

            if (!promo.IsStackable && hasNonStackableLineApplied)
                continue; // Không stackable cùng phạm vi line-item

            var discount = ApplyLinePromotion(promo, workingItems, appliedVoucher);
            if (discount > 0)
            {
                var appliedResult = new AppliedPromotionResult(
                    PromotionId: promo.Id,
                    PromotionName: promo.Name,
                    Type: promo.Type,
                    DiscountAmount: discount,
                    Description: $"Áp dụng khuyến mãi {promo.Name}",
                    VoucherId: appliedVoucher?.PromotionId == promo.Id ? appliedVoucher.Id : null);

                appliedPromotions.Add(appliedResult);

                orderDiscounts.Add(new OrderDiscountDto(
                    Id: Guid.NewGuid(),
                    PromotionId: promo.Id,
                    VoucherId: appliedVoucher?.PromotionId == promo.Id ? appliedVoucher.Id : null,
                    DiscountAmount: discount,
                    Description: promo.Name,
                    AppliedAt: DateTimeOffset.UtcNow));

                if (promo.IsExclusive)
                {
                    hasExclusiveApplied = true;
                    break;
                }

                if (!promo.IsStackable)
                {
                    hasNonStackableLineApplied = true;
                }
            }
        }

        // 4. Đánh giá khuyến mãi toàn đơn (Cart-level)
        if (!hasExclusiveApplied)
        {
            foreach (var promo in cartPromotions)
            {
                if (hasExclusiveApplied)
                    break;

                if (promo.IsExclusive && appliedPromotions.Count > 0)
                    continue;

                if (!promo.IsStackable && hasNonStackableCartApplied)
                    continue;

                decimal remainingSubtotal = workingItems.Sum(i => i.RemainingAmount);
                if (remainingSubtotal <= 0)
                    break;

                var discount = ApplyCartPromotion(promo, workingItems, remainingSubtotal);
                if (discount > 0)
                {
                    var appliedResult = new AppliedPromotionResult(
                        PromotionId: promo.Id,
                        PromotionName: promo.Name,
                        Type: promo.Type,
                        DiscountAmount: discount,
                        Description: $"Áp dụng khuyến mãi toàn đơn {promo.Name}",
                        VoucherId: appliedVoucher?.PromotionId == promo.Id ? appliedVoucher.Id : null);

                    appliedPromotions.Add(appliedResult);

                    orderDiscounts.Add(new OrderDiscountDto(
                        Id: Guid.NewGuid(),
                        PromotionId: promo.Id,
                        VoucherId: appliedVoucher?.PromotionId == promo.Id ? appliedVoucher.Id : null,
                        DiscountAmount: discount,
                        Description: promo.Name,
                        AppliedAt: DateTimeOffset.UtcNow));

                    if (promo.IsExclusive)
                    {
                        hasExclusiveApplied = true;
                        break;
                    }

                    if (!promo.IsStackable)
                    {
                        hasNonStackableCartApplied = true;
                    }
                }
            }
        }

        // 5. Tổng kết và bảo toàn các bất biến nghiệp vụ
        decimal totalDiscount = Math.Min(cart.Subtotal, appliedPromotions.Sum(p => p.DiscountAmount));
        decimal grandTotal = Math.Max(0, cart.Subtotal - totalDiscount);

        var itemDiscounts = workingItems.Select(i => new CartItemDiscountResult(
            SkuId: i.SkuId,
            OriginalLineTotal: i.OriginalLineTotal,
            DiscountAmount: i.DiscountAmount,
            FinalLineTotal: Math.Max(0, i.OriginalLineTotal - i.DiscountAmount)
        )).ToList();

        return new PromotionResult(
            Subtotal: cart.Subtotal,
            TotalDiscount: totalDiscount,
            GrandTotal: grandTotal,
            AppliedPromotions: appliedPromotions,
            ItemDiscounts: itemDiscounts,
            OrderDiscounts: orderDiscounts);
    }

    /// <summary>Determines whether a promotion applies at the cart-line level.</summary>
    private static bool IsLineLevelPromotion(Promotion promo)
    {
        return promo.Type switch
        {
            PromotionType.PercentSku => true,
            PromotionType.FixedSku => true,
            PromotionType.BuyXGetY => true,
            PromotionType.HappyHour => promo.AppliesTo != PromotionAppliesTo.All,
            _ => false
        };
    }

    /// <summary>Checks the promotion's active window, store, threshold, schedule, and targets.</summary>
    private static bool IsEligible(Promotion promo, PromotionCart cart, DateTime now, Voucher? appliedVoucher)
    {
        if (!promo.IsActiveAt(now))
            return false;

        if (promo.StoreId != Guid.Empty && promo.StoreId != cart.StoreId)
            return false;

        if (cart.Subtotal < promo.MinOrderAmount)
            return false;

        // Kiểm tra HappyHour điều kiện giờ trong ngày và ngày trong tuần
        if (promo.Type == PromotionType.HappyHour && !string.IsNullOrWhiteSpace(promo.ConditionsJson))
        {
            try
            {
                var condition = JsonSerializer.Deserialize<HappyHourCondition>(promo.ConditionsJson, JsonOptions);
                if (condition is not null)
                {
                    var timeOfDay = now.TimeOfDay;
                    if (condition.StartTime.HasValue && timeOfDay < condition.StartTime.Value)
                        return false;
                    if (condition.EndTime.HasValue && timeOfDay > condition.EndTime.Value)
                        return false;
                    if (condition.DaysOfWeek is { Count: > 0 } && !condition.DaysOfWeek.Contains(now.DayOfWeek))
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // Kiểm tra target SKU hoặc Category
        if (promo.AppliesTo == PromotionAppliesTo.SKU)
        {
            var targetSkuIds = promo.Targets.Where(t => t.SkuId.HasValue).Select(t => t.SkuId!.Value).ToHashSet();
            if (targetSkuIds.Count > 0 && !cart.Items.Any(i => targetSkuIds.Contains(i.SkuId)))
                return false;
        }
        else if (promo.AppliesTo == PromotionAppliesTo.Category)
        {
            var targetCategoryIds = promo.Targets.Where(t => t.CategoryId.HasValue).Select(t => t.CategoryId!.Value).ToHashSet();
            if (targetCategoryIds.Count > 0 && !cart.Items.Any(i => targetCategoryIds.Contains(i.CategoryId)))
                return false;
        }

        return true;
    }

    /// <summary>Applies a line-level promotion and returns its total discount.</summary>
    private static decimal ApplyLinePromotion(
        Promotion promo,
        List<WorkingCartItem> items,
        Voucher? appliedVoucher)
    {
        var eligibleItems = FilterEligibleItems(promo, items);
        if (eligibleItems.Count == 0)
            return 0;

        decimal totalPromoDiscount = 0;

        switch (promo.Type)
        {
            case PromotionType.PercentSku:
            {
                decimal rate = promo.Value > 1m ? promo.Value / 100m : promo.Value;
                foreach (var item in eligibleItems)
                {
                    if (item.RemainingAmount <= 0) continue;
                    decimal itemDiscount = Math.Round(item.RemainingAmount * rate, 2);
                    itemDiscount = Math.Min(itemDiscount, item.RemainingAmount);

                    item.DiscountAmount += itemDiscount;
                    totalPromoDiscount += itemDiscount;
                }
                break;
            }

            case PromotionType.FixedSku:
            {
                decimal fixedValue = promo.Value;
                foreach (var item in eligibleItems)
                {
                    if (item.RemainingAmount <= 0) continue;
                    decimal itemDiscount = Math.Min(item.RemainingAmount, fixedValue * item.Quantity);

                    item.DiscountAmount += itemDiscount;
                    totalPromoDiscount += itemDiscount;
                }
                break;
            }

            case PromotionType.BuyXGetY:
            {
                var condition = ParseBuyXGetYCondition(promo.ConditionsJson);
                int buyQty = Math.Max(1, condition.BuyQuantity);
                int getQty = Math.Max(1, condition.GetQuantity);
                decimal discountPercent = condition.DiscountPercent > 0 ? condition.DiscountPercent : 100;

                if (condition.FreeSkuId.HasValue)
                {
                    // Mua SKU A tặng SKU B
                    decimal totalQualifyingQty = eligibleItems.Sum(i => i.Quantity);
                    var giftItem = items.FirstOrDefault(i => i.SkuId == condition.FreeSkuId.Value);
                    if (giftItem != null && giftItem.RemainingAmount > 0)
                    {
                        int sets = (int)Math.Floor(totalQualifyingQty / buyQty);
                        decimal freeUnits = Math.Min(giftItem.Quantity, sets * getQty);
                        decimal giftDiscount = Math.Round(freeUnits * giftItem.UnitPrice * (discountPercent / 100m), 2);
                        giftDiscount = Math.Min(giftDiscount, giftItem.RemainingAmount);

                        giftItem.DiscountAmount += giftDiscount;
                        totalPromoDiscount += giftDiscount;
                    }
                }
                else
                {
                    // Mua X tặng Y trên cùng sản phẩm
                    int groupSize = buyQty + getQty;
                    foreach (var item in eligibleItems)
                    {
                        if (item.RemainingAmount <= 0) continue;
                        int sets = (int)Math.Floor(item.Quantity / groupSize);
                        decimal freeUnits = sets * getQty;
                        decimal itemDiscount = Math.Round(freeUnits * item.UnitPrice * (discountPercent / 100m), 2);
                        itemDiscount = Math.Min(itemDiscount, item.RemainingAmount);

                        item.DiscountAmount += itemDiscount;
                        totalPromoDiscount += itemDiscount;
                    }
                }
                break;
            }

            case PromotionType.HappyHour:
            {
                // HappyHour áp dụng theo dòng hàng
                decimal rate = promo.Value > 1m ? promo.Value / 100m : promo.Value;
                foreach (var item in eligibleItems)
                {
                    if (item.RemainingAmount <= 0) continue;
                    decimal itemDiscount = Math.Round(item.RemainingAmount * rate, 2);
                    itemDiscount = Math.Min(itemDiscount, item.RemainingAmount);

                    item.DiscountAmount += itemDiscount;
                    totalPromoDiscount += itemDiscount;
                }
                break;
            }
        }

        // Áp dụng trần giảm tối đa nếu có cấu hình MaxDiscountAmount
        if (promo.MaxDiscountAmount.HasValue && totalPromoDiscount > promo.MaxDiscountAmount.Value)
        {
            decimal excess = totalPromoDiscount - promo.MaxDiscountAmount.Value;
            // Thu hồi phần vượt quá từ các item đã giảm
            foreach (var item in eligibleItems.Where(i => i.DiscountAmount > 0))
            {
                if (excess <= 0) break;
                decimal reduce = Math.Min(excess, item.DiscountAmount);
                item.DiscountAmount -= reduce;
                excess -= reduce;
            }
            totalPromoDiscount = promo.MaxDiscountAmount.Value;
        }

        return totalPromoDiscount;
    }

    /// <summary>Applies a cart-level promotion across the remaining item balances.</summary>
    private static decimal ApplyCartPromotion(
        Promotion promo,
        List<WorkingCartItem> items,
        decimal remainingSubtotal)
    {
        decimal discount = 0;

        if (promo.Type == PromotionType.CartPercent || promo.Type == PromotionType.HappyHour)
        {
            decimal rate = promo.Value > 1m ? promo.Value / 100m : promo.Value;
            discount = Math.Round(remainingSubtotal * rate, 2);
        }
        else if (promo.Type == PromotionType.CartFixed)
        {
            discount = promo.Value;
        }

        discount = Math.Min(discount, remainingSubtotal);

        if (promo.MaxDiscountAmount.HasValue)
        {
            discount = Math.Min(discount, promo.MaxDiscountAmount.Value);
        }

        if (discount <= 0)
            return 0;

        // Phân bổ chiết khấu toàn đơn tỷ lệ thuận theo giá trị còn lại của từng dòng
        decimal allocated = 0;
        var activeItems = items.Where(i => i.RemainingAmount > 0).ToList();

        for (int idx = 0; idx < activeItems.Count; idx++)
        {
            var item = activeItems[idx];
            decimal share;
            if (idx == activeItems.Count - 1)
            {
                share = discount - allocated; // Đảm bảo làm tròn không bị lệch 1 đồng
            }
            else
            {
                share = Math.Round(discount * (item.RemainingAmount / remainingSubtotal), 2);
                allocated += share;
            }

            share = Math.Min(share, item.RemainingAmount);
            item.DiscountAmount += share;
        }

        return discount;
    }

    /// <summary>Returns the cart items targeted by the promotion.</summary>
    private static List<WorkingCartItem> FilterEligibleItems(Promotion promo, List<WorkingCartItem> items)
    {
        if (promo.AppliesTo == PromotionAppliesTo.All)
            return items;

        if (promo.AppliesTo == PromotionAppliesTo.SKU)
        {
            var skuIds = promo.Targets.Where(t => t.SkuId.HasValue).Select(t => t.SkuId!.Value).ToHashSet();
            return items.Where(i => skuIds.Contains(i.SkuId)).ToList();
        }

        if (promo.AppliesTo == PromotionAppliesTo.Category)
        {
            var categoryIds = promo.Targets.Where(t => t.CategoryId.HasValue).Select(t => t.CategoryId!.Value).ToHashSet();
            return items.Where(i => categoryIds.Contains(i.CategoryId)).ToList();
        }

        return items;
    }

    /// <summary>Parses a buy-X-get-Y condition, falling back to safe defaults.</summary>
    private static BuyXGetYCondition ParseBuyXGetYCondition(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new BuyXGetYCondition();

        try
        {
            return JsonSerializer.Deserialize<BuyXGetYCondition>(json, JsonOptions) ?? new BuyXGetYCondition();
        }
        catch
        {
            return new BuyXGetYCondition();
        }
    }

    /// <summary>Tracks mutable discount state for a cart item during evaluation.</summary>
    private sealed class WorkingCartItem(
        Guid SkuId,
        string SkuCode,
        Guid CategoryId,
        decimal Quantity,
        decimal UnitPrice,
        decimal OriginalLineTotal)
    {
        public Guid SkuId { get; } = SkuId;
        public string SkuCode { get; } = SkuCode;
        public Guid CategoryId { get; } = CategoryId;
        public decimal Quantity { get; } = Quantity;
        public decimal UnitPrice { get; } = UnitPrice;
        public decimal OriginalLineTotal { get; } = OriginalLineTotal;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal RemainingAmount => Math.Max(0, OriginalLineTotal - DiscountAmount);
    }
}
