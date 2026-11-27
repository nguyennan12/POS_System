using System.Globalization;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Promotions.Services;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services.Models;

namespace POS.Application.UseCases.Promotions.Queries;

public sealed class PromotionDetailsReader(
    ICategoryRepository categories,
    ISkuRepository skus,
    ILogger<PromotionDetailsReader> logger)
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    internal async Task<IReadOnlyList<ActivePromotionDetailDto>> ReadAsync(
        Guid storeId, IReadOnlyList<Promotion> promotions, CancellationToken cancellationToken)
    {
        var conditions = promotions.Where(p => p.Type == PromotionType.BuyXGetY)
            .ToDictionary(p => p.Id, p => PromotionConditions.Read<BuyXGetYCondition>(p, logger));
        var targets = promotions.SelectMany(p => p.Targets).ToList();
        var giftIds = conditions.Values.OfType<BuyXGetYCondition>().Select(c => c.FreeSkuId);
        var skuIds = targets.Select(t => t.SkuId).Concat(giftIds).OfType<Guid>().Distinct().ToList();
        var categoryIds = targets.Select(t => t.CategoryId).OfType<Guid>().ToHashSet();

        var skuNames = skuIds.Count == 0
            ? []
            : (await skus.GetByIdsWithProductAsync(skuIds, cancellationToken))
                .Where(s => s.StoreId == storeId)
                .ToDictionary(s => s.Id, s => s.Product is null ? s.SkuCode : $"{s.Product.Name} ({s.SkuCode})");
        var categoryNames = categoryIds.Count == 0
            ? []
            : (await categories.GetAllByStoreIdAsync(storeId, cancellationToken))
                .Where(c => c.StoreId == storeId && categoryIds.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.Name);

        return promotions.Select(promotion =>
        {
            var buyXGetY = conditions.GetValueOrDefault(promotion.Id);
            var happyHour = promotion.Type == PromotionType.HappyHour
                ? PromotionConditions.Read<HappyHourCondition>(promotion, logger)
                : null;
            var giftId = buyXGetY?.FreeSkuId;
            var giftName = giftId.HasValue ? skuNames.GetValueOrDefault(giftId.Value) : null;
            var targetCategories = ResolveTargets(promotion.Targets.Select(t => t.CategoryId), categoryNames);
            var targetSkus = ResolveTargets(promotion.Targets.Select(t => t.SkuId), skuNames);

            return new ActivePromotionDetailDto(
                promotion.Id, promotion.Name, promotion.Type, promotion.Value, promotion.AppliesTo,
                targetCategories, targetSkus, promotion.ValidFrom, promotion.ValidTo,
                promotion.MinOrderAmount, promotion.MaxDiscountAmount, promotion.Priority,
                promotion.IsStackable, promotion.IsExclusive,
                giftId.HasValue && giftName is null ? buyXGetY! with { FreeSkuId = null } : buyXGetY,
                giftName, happyHour,
                Describe(promotion, buyXGetY, giftName, happyHour, targetCategories, targetSkus));
        }).ToList();
    }

    private static List<PromotionTargetDto> ResolveTargets(
        IEnumerable<Guid?> ids, Dictionary<Guid, string> names) =>
        ids.OfType<Guid>().Distinct().Where(names.ContainsKey)
            .Select(id => new PromotionTargetDto(id, names[id])).ToList();

    private static string Describe(
        Promotion promotion, BuyXGetYCondition? buy, string? giftName, HappyHourCondition? happy,
        IReadOnlyList<PromotionTargetDto> categories, IReadOnlyList<PromotionTargetDto> skus)
    {
        var percent = Number(promotion.Value <= 1 ? promotion.Value * 100 : promotion.Value);
        var value = Number(promotion.Value);
        var offer = promotion.Type switch
        {
            PromotionType.CartPercent => $"Giảm {percent}% trên giá trị đơn đủ điều kiện",
            PromotionType.CartFixed => $"Giảm {value} đ trên đơn",
            PromotionType.PercentSku => $"Giảm {percent}% theo dòng hàng",
            PromotionType.FixedSku => $"Giảm {value} đ mỗi sản phẩm đủ điều kiện",
            PromotionType.BuyXGetY when buy is not null =>
                $"Mua {Math.Max(1, buy.BuyQuantity)}, nhận {Math.Max(1, buy.GetQuantity)} " +
                $"{(giftName ?? (buy.FreeSkuId is null ? "sản phẩm cùng loại" : "sản phẩm quà tặng"))} " +
                $"với mức giảm {Number(buy.DiscountPercent > 0 ? buy.DiscountPercent : 100)}%",
            PromotionType.BuyXGetY => "Khuyến mãi mua X tặng Y",
            PromotionType.HappyHour => $"Giảm {percent}% trong khung giờ vàng{DescribeHours(happy)}",
            _ => promotion.Name
        };

        var targets = promotion.AppliesTo == PromotionAppliesTo.Category ? categories : skus;
        if (targets.Count > 0)
            offer += $". Áp dụng cho {string.Join(", ", targets.Select(t => t.Name))}";
        else if (promotion.AppliesTo == PromotionAppliesTo.All)
            offer += ". Áp dụng cho tất cả sản phẩm";

        if (promotion.MinOrderAmount > 0)
            offer += $". Đơn tối thiểu {Number(promotion.MinOrderAmount)} đ";
        if (promotion.MaxDiscountAmount is decimal cap)
            offer += $". Giảm tối đa {Number(cap)} đ";

        return offer + ".";
    }

    private static string DescribeHours(HappyHourCondition? condition)
    {
        if (condition is null)
            return string.Empty;

        var details = new List<string>();
        if (condition.StartTime is TimeSpan start)
            details.Add($"từ {start:hh\\:mm} UTC");
        if (condition.EndTime is TimeSpan end)
            details.Add($"đến {end:hh\\:mm} UTC");
        if (condition.DaysOfWeek is { Count: > 0 } days)
            details.Add(string.Join(", ", days.Where(day => Enum.IsDefined(day)).Select(Vietnamese.DateTimeFormat.GetDayName)));

        return details.Count == 0 ? string.Empty : " " + string.Join(" ", details);
    }

    private static string Number(decimal number) => number.ToString("#,0.############################", Vietnamese);
}
