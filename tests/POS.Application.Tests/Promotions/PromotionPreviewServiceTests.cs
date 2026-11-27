using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Services;
using POS.Application.UseCases.Promotions;
using POS.Application.UseCases.Promotions.Services;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services;

namespace POS.Application.Tests.Promotions;

public sealed class PromotionPreviewServiceTests
{
    private readonly Guid storeId = Guid.NewGuid();
    private readonly ICurrentUser user = Substitute.For<ICurrentUser>();
    private readonly IStoreAccessPolicy storeAccess = Substitute.For<IStoreAccessPolicy>();
    private readonly IOrderRepository orders = Substitute.For<IOrderRepository>();
    private readonly ISkuRepository skus = Substitute.For<ISkuRepository>();
    private readonly IPromotionRepository promotions = Substitute.For<IPromotionRepository>();
    private readonly IVoucherRepository vouchers = Substitute.For<IVoucherRepository>();
    private readonly ICustomerRepository customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PromotionEngine engine = new();
    private readonly IPromotionPreviewService service;

    public PromotionPreviewServiceTests()
    {
        user.IsAuthenticated.Returns(true);
        storeAccess.EnforceStoreAccess().Returns(Result<Guid>.Success(storeId));
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICurrentUser)).Returns(user);
        services.GetService(typeof(IStoreAccessPolicy)).Returns(storeAccess);
        services.GetService(typeof(IOrderRepository)).Returns(orders);
        services.GetService(typeof(ISkuRepository)).Returns(skus);
        services.GetService(typeof(IPromotionRepository)).Returns(promotions);
        services.GetService(typeof(IVoucherRepository)).Returns(vouchers);
        services.GetService(typeof(ICustomerRepository)).Returns(customers);
        services.GetService(typeof(IPromotionEngine)).Returns(engine);
        services.GetService(typeof(IUnitOfWork)).Returns(unitOfWork);
        services.GetService(typeof(ILogger<PromotionPreviewService>)).Returns(NullLogger<PromotionPreviewService>.Instance);
        service = ActivatorUtilities.CreateInstance<PromotionPreviewService>(services);
    }

    [Fact]
    public async Task PreviewOrder_preserves_existing_order_and_voucher_and_matches_checkout_totals()
    {
        var sku = new Sku(Guid.NewGuid(), storeId, "SKU", "01", 100m, taxRate: 10m);
        var order = Order.CreateDraft(storeId, Guid.NewGuid(), Guid.NewGuid());
        var item = order.AddOrUpdateItem(sku, 2);
        item.ApplyDiscountAndTax(5m, 10m);
        sku.Update(sku.SkuCode, sku.Barcode, 150m, 0m, 10m, true, null);
        var voucherPromotion = new Promotion(storeId, "Voucher", PromotionType.CartFixed, 25m);
        var voucher = new Voucher(voucherPromotion.Id, "VOUCHER", 10);
        typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, voucherPromotion);
        order.SetAppliedVoucher(voucher.Id, "stored-code");

        orders.GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        skus.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([sku]);
        vouchers.GetByIdWithPromotionAsync(voucher.Id, Arg.Any<CancellationToken>()).Returns(voucher);
        SetAutomaticPromotions(new Promotion(storeId, "Giảm theo SKU", PromotionType.PercentSku, 10m, isStackable: true));

        var result = await service.PreviewOrderAsync(order.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var preview = result.Value!;
        preview.Subtotal.Should().Be(200m);
        preview.TotalDiscount.Should().Be(45m);
        preview.TaxTotal.Should().Be(15.50m);
        preview.GrandTotal.Should().Be(170.50m);
        preview.AppliedPromotions.Should().Contain(p => p.VoucherId == voucher.Id);

        order.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
        item.Qty.Should().Be(2m);
        item.UnitPrice.Should().Be(100m);
        item.DiscountAmount.Should().Be(5m);
        item.TaxAmount.Should().Be(19.50m);
        item.LineTotal.Should().Be(214.50m);
        order.Discounts.Should().BeEmpty();
        (order.Subtotal, order.DiscountTotal, order.TaxTotal, order.GrandTotal).Should().Be((0m, 0m, 0m, 0m));
        order.AppliedVoucherId.Should().Be(voucher.Id);
        order.AppliedVoucherCode.Should().Be("stored-code");
        voucher.UsedCount.Should().Be(0);
        orders.ReceivedCalls().Should().ContainSingle()
            .Which.GetMethodInfo().Name.Should().Be(nameof(IOrderRepository.GetByIdWithDetailsAsync));
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);

        var checkout = new CartCalculationService(skus, promotions, vouchers, customers, engine);
        (await checkout.RecalculateAsync(order, revalidateVoucher: true)).IsSuccess.Should().BeTrue();
        (order.Subtotal, order.DiscountTotal, order.TaxTotal, order.GrandTotal).Should()
            .Be((preview.Subtotal, preview.TotalDiscount, preview.TaxTotal, preview.GrandTotal));
    }

    [Theory]
    [InlineData(1, false, 1, 0)]
    [InlineData(2, false, 0, 1)]
    [InlineData(2, true, 0, 1)]
    public async Task PreviewCart_reports_missing_purchase_or_gift_without_saving(
        int quantity, bool separateGift, int missingQuantity, int missingGiftQuantity)
    {
        var sku = new Sku(Guid.NewGuid(), storeId, "SKU", "01", 100m);
        var gift = new Sku(Guid.NewGuid(), storeId, "GIFT", "02", 60m);
        var eligibleGiftId = separateGift ? gift.Id : sku.Id;
        var promotion = new Promotion(storeId, "Mua 2 tặng 1", PromotionType.BuyXGetY, 1m,
            conditionsJson: separateGift
                ? $$"""{"BuyQuantity":2,"GetQuantity":1,"FreeSkuId":"{{gift.Id}}"}"""
                : """{"BuyQuantity":2,"GetQuantity":1}""",
            appliesTo: PromotionAppliesTo.SKU);
        promotion.AddTargetSku(sku.Id);
        skus.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => new[] { sku, gift }.Where(item => call.Arg<IEnumerable<Guid>>().Contains(item.Id)).ToList());
        SetAutomaticPromotions(promotion);

        var result = await service.PreviewCartAsync(new PromotionPreviewCart([new(sku.Id, quantity)], null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalDiscount.Should().Be(0);
        var suggestion = result.Value.Suggestions.Should().ContainSingle().Subject;
        suggestion.PromotionId.Should().Be(promotion.Id);
        suggestion.FreeSkuId.Should().Be(eligibleGiftId);
        suggestion.MissingQuantity.Should().Be(missingQuantity);
        suggestion.MissingGiftQuantity.Should().Be(missingGiftQuantity);
        orders.ReceivedCalls().Should().BeEmpty();
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Preview_excludes_orders_and_skus_from_another_store()
    {
        var foreignStore = Guid.NewGuid();
        var order = Order.CreateDraft(foreignStore, Guid.NewGuid(), Guid.NewGuid());
        var sku = new Sku(Guid.NewGuid(), foreignStore, "SKU", "01", 100m);
        orders.GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        skus.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([sku]);

        var orderResult = await service.PreviewOrderAsync(order.Id, CancellationToken.None);
        var cartResult = await service.PreviewCartAsync(new PromotionPreviewCart([new(sku.Id, 1)], null, null),
            CancellationToken.None);

        orderResult.Error.Should().Be(OrderErrors.OrderNotFound);
        cartResult.Error.Should().Be(OrderErrors.SkuNotFound);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private void SetAutomaticPromotions(params Promotion[] active) =>
        promotions.GetActiveAutomaticPromotionsAsync(storeId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(active);
}
