using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Services;
using POS.Application.UseCases.Promotions;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Rbac.Constants;

namespace POS.Application.Tests.AI;

public sealed class PromotionAiToolTests
{
    [Fact]
    public void AllExtendedToolsResolveUnderTheirSchemaNames()
    {
        using var provider = new AiToolTestHost().CreateServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var schemas = provider.GetRequiredService<IAiToolRegistry>().GetAvailableTools().Skip(10);
        var names = scope.ServiceProvider.GetServices<IAiToolHandler>().Select(handler => handler.Name).ToArray();

        foreach (var schema in schemas)
            Assert.Single(names, name => name == schema.Name);
    }

    [Fact]
    public async Task VoucherToolRejectsCallerSuppliedSubtotal()
    {
        using var provider = new AiToolTestHost().CreateServices().BuildServiceProvider();
        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.ValidateVoucher,
            $$"""{"orderId":"{{Guid.NewGuid()}}","code":"DEMO","subtotal":100}""");

        Assert.Equal(AiToolErrors.InvalidArguments, result.ErrorCode);
    }

    [Theory]
    [InlineData(AiToolNames.ValidateVoucher, true)]
    [InlineData(AiToolNames.ValidateVoucher, false)]
    [InlineData(AiToolNames.PreviewOrderPromotions, true)]
    [InlineData(AiToolNames.PreviewOrderPromotions, false)]
    public async Task CashierReadOwnCanPreviewOnlyTheirOwnOrder(string tool, bool ownOrder)
    {
        var fixture = new Fixture("orders:read_own", "vouchers:read");
        var order = fixture.CreateOrder(ownOrder ? fixture.Host.EmployeeId : Guid.NewGuid());
        fixture.SetVoucher(new Promotion(fixture.Host.StoreId, "Ưu đãi", PromotionType.CartFixed, 10m));
        var services = fixture.CreateServices();
        fixture.Host.CurrentUser.Role.Returns(RoleNames.Cashier);
        using var provider = services.BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, tool, OrderArguments(tool, order.Id));

        if (ownOrder)
        {
            Assert.True(result.Success, result.ErrorCode);
            if (tool == AiToolNames.ValidateVoucher)
                Assert.True(Assert.IsType<VoucherPreviewResult>(result.Data).IsValid);
            else
                Assert.IsType<PromotionPreviewResult>(result.Data);
        }
        else
        {
            Assert.Equal(AiToolErrors.PermissionDenied, result.ErrorCode);
            Assert.Empty(fixture.Vouchers.ReceivedCalls());
            Assert.Empty(fixture.Skus.ReceivedCalls());
        }
    }

    [Fact]
    public async Task VoucherToolRequiresVoucherPermissionAsWellAsOrderPermission()
    {
        var fixture = new Fixture("orders:read");
        var order = fixture.CreateOrder();
        using var provider = fixture.CreateServices().BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.ValidateVoucher,
            OrderArguments(AiToolNames.ValidateVoucher, order.Id));

        Assert.Equal(AiToolErrors.PermissionDenied, result.ErrorCode);
        await fixture.Orders.DidNotReceive().GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AiToolNames.ValidateVoucher)]
    [InlineData(AiToolNames.PreviewOrderPromotions)]
    public async Task ChainOwnerCannotPreviewOrdersOutsideTheSelectedStore(string tool)
    {
        var fixture = new Fixture("orders:read", "vouchers:read");
        var order = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), fixture.Host.EmployeeId);
        fixture.Orders.GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var services = fixture.CreateServices();
        fixture.Host.CurrentUser.IsChainOwner.Returns(true);
        fixture.Host.Employees.GetByIdAsync(fixture.Host.EmployeeId, Arg.Any<CancellationToken>()).Returns(
            new Employee("Chủ chuỗi", "owner", "hash", "pin", Guid.NewGuid(),
                isChainOwner: true, id: fixture.Host.EmployeeId));
        using var provider = services.BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, tool, OrderArguments(tool, order.Id));

        Assert.Equal(AiToolErrors.NotFound, result.ErrorCode);
        Assert.Empty(fixture.Vouchers.ReceivedCalls());
    }

    [Theory]
    [InlineData(PromotionType.CartPercent, 30)]
    [InlineData(PromotionType.CartFixed, 10)]
    [InlineData(PromotionType.PercentSku, 30)]
    [InlineData(PromotionType.FixedSku, 30)]
    [InlineData(PromotionType.BuyXGetY, 100)]
    [InlineData(PromotionType.HappyHour, 30)]
    public async Task VoucherPreviewUsesTheStoredOrderPriceAndMatchesCheckoutWithoutChangingTheOrder(
        PromotionType type, int discount)
    {
        var fixture = new Fixture("orders:read", "vouchers:read");
        var order = fixture.CreateOrder();
        var existingVoucherId = Guid.NewGuid();
        order.SetAppliedVoucher(existingVoucherId, "EXISTING");
        var item = Assert.Single(order.Items);
        item.ApplyDiscountAndTax(5, 10);
        var originalAmounts = (item.UnitPrice, item.DiscountAmount, item.TaxAmount, item.LineTotal);
        fixture.Sku.Update(fixture.Sku.SkuCode, fixture.Sku.Barcode, 900, 0, 10, true, null);
        var promotion = new Promotion(fixture.Host.StoreId, "Ưu đãi", type, 10m, conditionsJson: type switch
        {
            PromotionType.BuyXGetY => """{"BuyQuantity":2,"GetQuantity":1}""",
            PromotionType.HappyHour => "{}",
            _ => null
        });
        var voucher = fixture.SetVoucher(promotion);
        using var provider = fixture.CreateServices().BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.ValidateVoucher,
            OrderArguments(AiToolNames.ValidateVoucher, order.Id));

        Assert.True(result.Success, result.ErrorCode);
        var preview = Assert.IsType<VoucherPreviewResult>(result.Data);
        Assert.True(preview.IsValid);
        Assert.Equal(discount, preview.DiscountAmount);
        Assert.Equal(promotion.Name, preview.PromotionName);
        Assert.Equal(existingVoucherId, order.AppliedVoucherId);
        Assert.Equal("EXISTING", order.AppliedVoucherCode);
        Assert.Empty(order.Discounts);
        Assert.Equal(0, order.GrandTotal);
        Assert.Equal(originalAmounts, (item.UnitPrice, item.DiscountAmount, item.TaxAmount, item.LineTotal));
        Assert.Equal(0, voucher.UsedCount);
        await fixture.Vouchers.DidNotReceive().GetByIdWithPromotionAsync(existingVoucherId, Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);

        using var scope = provider.CreateScope();
        var checkout = scope.ServiceProvider.GetRequiredService<ICartCalculationService>();
        Assert.True((await checkout.RecalculateAsync(order, voucher, revalidateVoucher: true)).IsSuccess);
        Assert.Equal(300, order.Subtotal);
        Assert.Equal(preview.GrandTotal, order.GrandTotal);
        Assert.Equal(preview.DiscountAmount, order.DiscountTotal);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("expired")]
    [InlineData("global limit")]
    [InlineData("customer limit")]
    [InlineData("other store")]
    [InlineData("conditions not met")]
    public async Task VoucherPreviewReportsWhyTheVoucherCannotBeApplied(string scenario)
    {
        var fixture = new Fixture("orders:read", "vouchers:read");
        var customerId = Guid.NewGuid();
        var order = fixture.CreateOrder();
        order.SetCustomer(customerId);
        var storeId = scenario == "other store" ? Guid.NewGuid() : fixture.Host.StoreId;
        var promotion = new Promotion(storeId, "Ưu đãi", PromotionType.CartFixed, 10m,
            minOrderAmount: scenario == "conditions not met" ? 1000m : 0m);
        var voucher = fixture.SetVoucher(promotion, isActive: scenario != "inactive",
            expiresAt: scenario == "expired" ? DateTime.UtcNow.AddDays(-1) : null);
        if (scenario == "global limit")
            voucher.RecordUse();
        if (scenario == "customer limit")
            fixture.Vouchers.GetCustomerUsageCountAsync(voucher.Id, customerId, Arg.Any<CancellationToken>()).Returns(1);
        using var provider = fixture.CreateServices().BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.ValidateVoucher,
            OrderArguments(AiToolNames.ValidateVoucher, order.Id));

        var preview = Assert.IsType<VoucherPreviewResult>(result.Data);
        Assert.False(preview.IsValid);
        Assert.Equal(scenario switch
        {
            "inactive" => OrderErrors.VoucherInactive.Message,
            "expired" => OrderErrors.VoucherExpired.Message,
            "global limit" => OrderErrors.VoucherUsageLimitReached.Message,
            "customer limit" => OrderErrors.VoucherCustomerLimitReached.Message,
            "other store" => OrderErrors.VoucherNotFound.Message,
            _ => "Mã giảm giá không áp dụng được cho đơn hàng này."
        }, preview.Reason);
        Assert.Null(preview.PromotionName);
        Assert.Null(preview.GrandTotal);
        Assert.Null(order.AppliedVoucherId);
        await fixture.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ActivePromotionsUsesTheExistingQueryWithTypeAndLimit()
    {
        var fixture = new Fixture("promotions:read");
        var offer = new Promotion(fixture.Host.StoreId, "Ưu đãi khung giờ", PromotionType.HappyHour, 10m,
            conditionsJson: "invalid json");
        fixture.Promotions.GetActivePromotionsAsync(fixture.Host.StoreId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([new Promotion(fixture.Host.StoreId, "Giảm giá", PromotionType.CartFixed, 10m), offer, offer]);
        using var provider = fixture.CreateServices().BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.ListActivePromotions,
            """{"type":"HappyHour","limit":1}""");

        Assert.True(result.Success, result.ErrorCode);
        var promotion = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ActivePromotionDetailDto>>(result.Data));
        Assert.Equal(offer.Id, promotion.PromotionId);
        Assert.NotEmpty(promotion.DescriptionVi);
    }

    [Fact]
    public async Task SkuPromotionsMarksOffersPotentialAndRejectsForeignSkus()
    {
        var fixture = new Fixture("promotions:read");
        var foreignSku = new Sku(Guid.NewGuid(), Guid.NewGuid(), "OTHER", "02", 100m);
        fixture.Skus.GetByIdWithProductAsync(foreignSku.Id, Arg.Any<CancellationToken>()).Returns(foreignSku);
        var offer = new Promotion(fixture.Host.StoreId, "Mua 2 tặng 1", PromotionType.BuyXGetY, 1m,
            conditionsJson: """{"BuyQuantity":2,"GetQuantity":1}""");
        fixture.Promotions.GetActivePromotionsAsync(fixture.Host.StoreId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([offer]);
        using var provider = fixture.CreateServices().BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetProductPromotions,
            $$"""{"skuId":"{{fixture.Sku.Id}}","quantity":2.5}""");
        var foreignResult = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.GetProductPromotions,
            $$"""{"skuId":"{{foreignSku.Id}}"}""");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var data = JsonSerializer.SerializeToElement(result.Data, options);
        Assert.Equal("potential", data.GetProperty("eligibility").GetString());
        Assert.Equal(2.5m, data.GetProperty("quantity").GetDecimal());
        var promotion = Assert.Single(data.GetProperty("promotions").EnumerateArray());
        Assert.Equal(2, promotion.GetProperty("buyXGetY").GetProperty("buyQuantity").GetInt32());
        Assert.Equal(AiToolErrors.NotFound, foreignResult.ErrorCode);
    }

    [Fact]
    public async Task OrderPreviewReturnsBuyXGetYSuggestionsWithoutSaving()
    {
        var fixture = new Fixture("orders:read");
        var order = fixture.CreateOrder(quantity: 1);
        var offer = new Promotion(fixture.Host.StoreId, "Mua 2 tặng 1", PromotionType.BuyXGetY, 1m,
            conditionsJson: """{"BuyQuantity":2,"GetQuantity":1}""");
        fixture.Promotions.GetActiveAutomaticPromotionsAsync(
            fixture.Host.StoreId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([offer]);
        using var provider = fixture.CreateServices().BuildServiceProvider();

        var result = await AiToolTestHost.ExecuteAsync(provider, AiToolNames.PreviewOrderPromotions,
            OrderArguments(AiToolNames.PreviewOrderPromotions, order.Id));

        var preview = Assert.IsType<PromotionPreviewResult>(result.Data);
        Assert.Equal(100m, preview.Subtotal);
        Assert.Equal(110m, preview.GrandTotal);
        Assert.Equal(1m, Assert.Single(preview.Suggestions).MissingQuantity);
        Assert.Empty(order.Discounts);
        Assert.Equal(0m, order.GrandTotal);
        await fixture.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static string OrderArguments(string tool, Guid orderId) =>
        tool == AiToolNames.ValidateVoucher
            ? $$"""{"orderId":"{{orderId}}","code":"DEMO"}"""
            : $$"""{"orderId":"{{orderId}}"}""";

    private sealed class Fixture
    {
        public Fixture(params string[] permissions)
        {
            Host = new AiToolTestHost(permissions);
            Sku = new Sku(Guid.NewGuid(), Host.StoreId, "SKU", "01", 100m, taxRate: 10m);
            Skus.GetByIdWithProductAsync(Sku.Id, Arg.Any<CancellationToken>()).Returns(Sku);
            Skus.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([Sku]);
            Promotions.GetActiveAutomaticPromotionsAsync(Host.StoreId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Array.Empty<Promotion>());
        }

        public AiToolTestHost Host { get; }
        public Sku Sku { get; }
        public IOrderRepository Orders { get; } = Substitute.For<IOrderRepository>();
        public ISkuRepository Skus { get; } = Substitute.For<ISkuRepository>();
        public IPromotionRepository Promotions { get; } = Substitute.For<IPromotionRepository>();
        public IVoucherRepository Vouchers { get; } = Substitute.For<IVoucherRepository>();
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

        public Order CreateOrder(Guid? createdBy = null, decimal quantity = 3)
        {
            var order = Order.CreateDraft(Host.StoreId, Guid.NewGuid(), createdBy ?? Host.EmployeeId);
            order.AddOrUpdateItem(Sku, quantity);
            Orders.GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
            return order;
        }

        public Voucher SetVoucher(Promotion promotion, bool isActive = true, DateTime? expiresAt = null)
        {
            var voucher = new Voucher(promotion.Id, "DEMO", 1, expiresAt: expiresAt, isActive: isActive);
            typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, promotion);
            Vouchers.GetByCodeWithPromotionAsync(voucher.Code, Arg.Any<CancellationToken>()).Returns(voucher);
            return voucher;
        }

        public IServiceCollection CreateServices() => Host.CreateServices()
            .AddSingleton(Orders)
            .AddSingleton(Skus)
            .AddSingleton(Promotions)
            .AddSingleton(Vouchers)
            .AddSingleton(UnitOfWork)
            .AddSingleton(Substitute.For<ICustomerRepository>())
            .AddSingleton(Substitute.For<ICategoryRepository>());
    }
}
