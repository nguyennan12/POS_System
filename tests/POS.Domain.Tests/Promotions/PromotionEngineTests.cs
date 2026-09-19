using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services;
using POS.Domain.Promotions.Services.Models;
using Xunit;

namespace POS.Domain.Tests.Promotions;

public class PromotionEngineTests
{
    private readonly IPromotionEngine _engine = new PromotionEngine();
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _catBeverages = Guid.NewGuid();
    private readonly Guid _catFood = Guid.NewGuid();
    private readonly Guid _skuCoffee = Guid.NewGuid();
    private readonly Guid _skuTea = Guid.NewGuid();
    private readonly Guid _skuCake = Guid.NewGuid();

    /// <summary>Returns today's UTC date at the requested time.</summary>
    private DateTime TodayAt(int hour, int minute) =>
        DateTime.UtcNow.Date.AddHours(hour).AddMinutes(minute);

    /// <summary>Verifies percentage discounts for targeted SKUs.</summary>
    [Fact]
    public void Evaluate_PercentSku_CalculatesCorrectDiscount()
    {
        // Arrange
        var item1 = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 2, UnitPrice: 50_000m); // 100k
        var item2 = new PromotionCartItem(_skuCake, "SKU-CK", _catFood, Quantity: 1, UnitPrice: 40_000m); // 40k
        var cart = new PromotionCart(_storeId, [item1, item2]);

        var promo = new Promotion(
            _storeId,
            name: "Giảm 10% cà phê",
            type: PromotionType.PercentSku,
            value: 10m,
            appliesTo: PromotionAppliesTo.SKU);
        promo.AddTargetSku(_skuCoffee);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert
        result.Subtotal.Should().Be(140_000m);
        result.TotalDiscount.Should().Be(10_000m); // 10% của 100k
        result.GrandTotal.Should().Be(130_000m);
        result.AppliedPromotions.Should().HaveCount(1);
        result.AppliedPromotions[0].DiscountAmount.Should().Be(10_000m);
    }

    /// <summary>Verifies that percentage discounts honor their maximum amount.</summary>
    [Fact]
    public void Evaluate_PercentSku_CapsAtMaxDiscountAmount()
    {
        // Arrange
        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 10, UnitPrice: 100_000m); // 1M
        var cart = new PromotionCart(_storeId, [item]);

        var promo = new Promotion(
            _storeId,
            name: "Giảm 20% tối đa 50k",
            type: PromotionType.PercentSku,
            value: 20m, // 20% của 1M = 200k
            maxDiscountAmount: 50_000m,
            appliesTo: PromotionAppliesTo.SKU);
        promo.AddTargetSku(_skuCoffee);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert
        result.TotalDiscount.Should().Be(50_000m);
        result.GrandTotal.Should().Be(950_000m);
    }

    /// <summary>Verifies that fixed SKU discounts cannot exceed the item total.</summary>
    [Fact]
    public void Evaluate_FixedSku_CalculatesCorrectDiscount_AndCapsAtItemTotal()
    {
        // Arrange
        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 2, UnitPrice: 30_000m); // 60k
        var cart = new PromotionCart(_storeId, [item]);

        // Giảm 40k mỗi sản phẩm (tổng định giảm 80k nhưng dòng hàng chỉ có 60k)
        var promo = new Promotion(
            _storeId,
            name: "Giảm 40k cà phê",
            type: PromotionType.FixedSku,
            value: 40_000m,
            appliesTo: PromotionAppliesTo.SKU);
        promo.AddTargetSku(_skuCoffee);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert: Không bao giờ giảm vượt quá giá trị dòng hàng (60k)
        result.TotalDiscount.Should().Be(60_000m);
        result.GrandTotal.Should().Be(0m);
    }

    /// <summary>Verifies buy-X-get-Y discounts when the purchased and free SKU are the same.</summary>
    [Fact]
    public void Evaluate_BuyXGetY_SameSku_CalculatesFreeItemsCorrectly()
    {
        // Arrange: Mua 2 tặng 1 cùng loại cà phê
        var condition = new BuyXGetYCondition(BuyQuantity: 2, GetQuantity: 1, DiscountPercent: 100);
        var promo = new Promotion(
            _storeId,
            name: "Mua 2 tặng 1 cà phê",
            type: PromotionType.BuyXGetY,
            value: 0m,
            conditionsJson: JsonSerializer.Serialize(condition),
            appliesTo: PromotionAppliesTo.SKU);
        promo.AddTargetSku(_skuCoffee);

        // Khách mua 5 ly, đơn giá 30k/ly. Nhóm 3 (mua 2 tặng 1) => được 1 ly miễn phí = 30k giảm giá
        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 5, UnitPrice: 30_000m);
        var cart = new PromotionCart(_storeId, [item]);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert
        result.Subtotal.Should().Be(150_000m);
        result.TotalDiscount.Should().Be(30_000m);
        result.GrandTotal.Should().Be(120_000m);
    }

    /// <summary>Verifies buy-X-get-Y discounts when a different SKU receives the discount.</summary>
    [Fact]
    public void Evaluate_BuyXGetY_CrossSku_AppliesDiscountToGiftSku()
    {
        // Arrange: Mua 2 Cà phê tặng 1 Bánh ngọt giảm 50%
        var condition = new BuyXGetYCondition(BuyQuantity: 2, GetQuantity: 1, FreeSkuId: _skuCake, DiscountPercent: 50);
        var promo = new Promotion(
            _storeId,
            name: "Mua 2 Cà phê giảm 50% Bánh",
            type: PromotionType.BuyXGetY,
            value: 0m,
            conditionsJson: JsonSerializer.Serialize(condition),
            appliesTo: PromotionAppliesTo.SKU);
        promo.AddTargetSku(_skuCoffee);

        var coffee = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 2, UnitPrice: 40_000m); // 80k
        var cake = new PromotionCartItem(_skuCake, "SKU-CK", _catFood, Quantity: 1, UnitPrice: 50_000m); // 50k
        var cart = new PromotionCart(_storeId, [coffee, cake]);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert: Giảm 50% của 1 bánh 50k = 25k
        result.Subtotal.Should().Be(130_000m);
        result.TotalDiscount.Should().Be(25_000m);
        result.GrandTotal.Should().Be(105_000m);
    }

    /// <summary>Verifies percentage discounts on carts that meet the order threshold.</summary>
    [Fact]
    public void Evaluate_CartPercent_AppliesToEligibleSubtotal()
    {
        // Arrange
        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 5, UnitPrice: 100_000m); // 500k
        var cart = new PromotionCart(_storeId, [item]);

        var promo = new Promotion(
            _storeId,
            name: "Giảm 10% đơn từ 300k",
            type: PromotionType.CartPercent,
            value: 10m,
            minOrderAmount: 300_000m);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert
        result.TotalDiscount.Should().Be(50_000m);
        result.GrandTotal.Should().Be(450_000m);
    }

    /// <summary>Verifies that carts below the order threshold receive no discount.</summary>
    [Fact]
    public void Evaluate_CartPercent_WhenBelowMinOrderAmount_DoesNotApply()
    {
        // Arrange: Giỏ hàng chỉ 200k, min 300k
        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 2, UnitPrice: 100_000m);
        var cart = new PromotionCart(_storeId, [item]);

        var promo = new Promotion(
            _storeId,
            name: "Giảm 10% đơn từ 300k",
            type: PromotionType.CartPercent,
            value: 10m,
            minOrderAmount: 300_000m);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert
        result.TotalDiscount.Should().Be(0m);
        result.GrandTotal.Should().Be(200_000m);
        result.AppliedPromotions.Should().BeEmpty();
    }

    /// <summary>Verifies that fixed cart discounts cannot exceed the cart total.</summary>
    [Fact]
    public void Evaluate_CartFixed_CalculatesDiscountAndNeverExceedsTotal()
    {
        // Arrange: Khuyến mãi cố định 500k, nhưng đơn hàng chỉ 200k
        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 2, UnitPrice: 100_000m); // 200k
        var cart = new PromotionCart(_storeId, [item]);

        var promo = new Promotion(
            _storeId,
            name: "Voucher giảm 500k",
            type: PromotionType.CartFixed,
            value: 500_000m);

        // Act
        var result = _engine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert: Không bao giờ làm grand total < 0
        result.TotalDiscount.Should().Be(200_000m);
        result.GrandTotal.Should().Be(0m);
    }

    /// <summary>Verifies that happy-hour discounts apply only within their configured window.</summary>
    [Fact]
    public void Evaluate_HappyHour_AppliesWithinTimeRange_IgnoresOutsideTimeRange()
    {
        // Arrange: Giờ vàng từ 14:00 đến 17:00 giảm 20%
        var condition = new HappyHourCondition(
            StartTime: new TimeSpan(14, 0, 0),
            EndTime: new TimeSpan(17, 0, 0));

        var promo = new Promotion(
            _storeId,
            name: "Happy Hour 14h-17h",
            type: PromotionType.HappyHour,
            value: 20m,
            conditionsJson: JsonSerializer.Serialize(condition),
            validFrom: TodayAt(0, 0));

        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 1, UnitPrice: 100_000m);
        var cart = new PromotionCart(_storeId, [item]);

        // Act & Assert 1: Lúc 15:30 (trong giờ vàng) -> Được áp dụng
        var resultInHour = _engine.Evaluate(cart, [promo], TodayAt(15, 30));
        resultInHour.TotalDiscount.Should().Be(20_000m);

        // Act & Assert 2: Lúc 18:00 (hết giờ vàng) -> Không được áp dụng
        var resultOutHour = _engine.Evaluate(cart, [promo], TodayAt(18, 00));
        resultOutHour.TotalDiscount.Should().Be(0m);
    }

    /// <summary>Verifies that an exclusive promotion prevents later promotions from applying.</summary>
    [Fact]
    public void Evaluate_ExclusivePromotion_StopsFurtherPromotions()
    {
        // Arrange:
        // Promo 1: Độc quyền (Exclusive), Priority 10, giảm 20k
        var exclusivePromo = new Promotion(
            _storeId,
            name: "KM Độc quyền",
            type: PromotionType.CartFixed,
            value: 20_000m,
            priority: 10,
            isExclusive: true);

        // Promo 2: Priority 5, giảm 50k
        var otherPromo = new Promotion(
            _storeId,
            name: "KM Khác",
            type: PromotionType.CartFixed,
            value: 50_000m,
            priority: 5,
            isStackable: true);

        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 2, UnitPrice: 100_000m); // 200k
        var cart = new PromotionCart(_storeId, [item]);

        // Act
        var result = _engine.Evaluate(cart, [exclusivePromo, otherPromo], DateTime.UtcNow);

        // Assert: Chỉ áp dụng KM độc quyền, dừng toàn bộ KM sau
        result.AppliedPromotions.Should().HaveCount(1);
        result.AppliedPromotions[0].PromotionName.Should().Be("KM Độc quyền");
        result.TotalDiscount.Should().Be(20_000m);
    }

    /// <summary>Verifies that stackable line and cart promotions combine.</summary>
    [Fact]
    public void Evaluate_StackablePromotions_CombinesCorrectly()
    {
        // Arrange:
        // Promo 1: Giảm 10% cà phê (Line-item, Stackable, Priority 10)
        var linePromo = new Promotion(
            _storeId,
            name: "Giảm 10% Cà phê",
            type: PromotionType.PercentSku,
            value: 10m,
            priority: 10,
            isStackable: true,
            appliesTo: PromotionAppliesTo.SKU);
        linePromo.AddTargetSku(_skuCoffee);

        // Promo 2: Giảm thêm 10k toàn đơn (Cart-item, Stackable, Priority 5)
        var cartPromo = new Promotion(
            _storeId,
            name: "Giảm 10k Toàn đơn",
            type: PromotionType.CartFixed,
            value: 10_000m,
            priority: 5,
            isStackable: true);

        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 1, UnitPrice: 100_000m); // 100k
        var cart = new PromotionCart(_storeId, [item]);

        // Act
        var result = _engine.Evaluate(cart, [linePromo, cartPromo], DateTime.UtcNow);

        // Assert: 100k - 10k (line) - 10k (cart) = 80k Grand Total
        result.Subtotal.Should().Be(100_000m);
        result.TotalDiscount.Should().Be(20_000m);
        result.GrandTotal.Should().Be(80_000m);
        result.AppliedPromotions.Should().HaveCount(2);
    }

    /// <summary>Verifies that higher-priority non-stackable promotions apply first.</summary>
    [Fact]
    public void Evaluate_PriorityOrder_AppliesHigherPriorityFirst()
    {
        // Arrange: 2 KM không cộng dồn cùng phạm vi Cart, ưu tiên Priority 20 hơn Priority 10
        var lowPromo = new Promotion(
            _storeId,
            name: "Giảm 10k (Thấp)",
            type: PromotionType.CartFixed,
            value: 10_000m,
            priority: 10,
            isStackable: false);

        var highPromo = new Promotion(
            _storeId,
            name: "Giảm 30k (Cao)",
            type: PromotionType.CartFixed,
            value: 30_000m,
            priority: 20,
            isStackable: false);

        var item = new PromotionCartItem(_skuCoffee, "SKU-CF", _catBeverages, Quantity: 1, UnitPrice: 100_000m);
        var cart = new PromotionCart(_storeId, [item]);

        // Act
        var result = _engine.Evaluate(cart, [lowPromo, highPromo], DateTime.UtcNow);

        // Assert: KM Priority cao hơn được chọn, KM thấp hơn bị bỏ qua vì không stackable
        result.AppliedPromotions.Should().HaveCount(1);
        result.AppliedPromotions[0].PromotionName.Should().Be("Giảm 30k (Cao)");
        result.TotalDiscount.Should().Be(30_000m);
    }

    /// <summary>Verifies that promotion evaluation meets the performance requirement.</summary>
    [Fact]
    public void Evaluate_Performance_CompletesUnder5Milliseconds()
    {
        // Arrange: Giỏ hàng 10 sản phẩm, 5 promotion phức tạp
        var items = Enumerable.Range(1, 10).Select(i =>
            new PromotionCartItem(Guid.NewGuid(), $"SKU-{i}", _catBeverages, Quantity: 2, UnitPrice: 50_000m)
        ).ToList();
        var cart = new PromotionCart(_storeId, items);

        var promos = new List<Promotion>
        {
            new(_storeId, "P1", PromotionType.PercentSku, 10m, priority: 10, isStackable: true),
            new(_storeId, "P2", PromotionType.CartFixed, 20_000m, priority: 8, isStackable: true),
            new(_storeId, "P3", PromotionType.CartPercent, 5m, priority: 5, isStackable: true),
        };

        // Warm up JIT
        _engine.Evaluate(cart, promos, DateTime.UtcNow);

        // Act: Đong đếm thời gian thực thi
        var sw = Stopwatch.StartNew();
        var result = _engine.Evaluate(cart, promos, DateTime.UtcNow);
        sw.Stop();

        // Assert: Acceptance criteria < 5ms (trong thực tế in-memory thường < 0.1ms)
        sw.ElapsedMilliseconds.Should().BeLessThan(5);
        result.GrandTotal.Should().BeGreaterThan(0);
    }
}
