using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Stores;

namespace POS.Application.Tests.Invoices;

public class GenerateInvoiceTests
{
    private readonly IOrderRepository orders = Substitute.For<IOrderRepository>();
    private readonly IStoreRepository stores = Substitute.For<IStoreRepository>();
    private readonly IInvoiceRepository invoices = Substitute.For<IInvoiceRepository>();
    private readonly ILogger<GenerateInvoiceCommandHandler> logger = Substitute.For<ILogger<GenerateInvoiceCommandHandler>>();
    private readonly Store store = new("Invoice store");
    private readonly Order order;

    public GenerateInvoiceTests()
    {
        order = Order.CreateDraft(store.Id, Guid.NewGuid(), Guid.NewGuid());
        var product = new Product(store.Id, Guid.NewGuid(), "Product", "piece");
        var sku = new Sku(product.Id, store.Id, "INVOICE-SKU", "123456", 100, 50, 0, true) { Product = product };
        order.AddOrUpdateItem(sku, 1);
        order.ApplyPromotionEvaluation(new PromotionResult(100, 0, 100, [], [], []), new Dictionary<Guid, decimal>());
        orders.GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        stores.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        invoices.GetNextSequenceAsync(store.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(42);
    }

    private Task<Result> Generate() => new GenerateInvoiceCommandHandler(orders, stores, invoices, logger)
        .Handle(new(order.Id), default);

    private void MarkPaid()
    {
        Assert.True(order.Confirm().IsSuccess);
        order.ProcessPayments([(PaymentMethod.Cash, 100, null)]);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task Generate_ShouldRejectMissingOrder()
    {
        orders.GetByIdWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns((Order?)null);
        Assert.Equal(OrderErrors.OrderNotFound, (await Generate()).Error);
        await invoices.DidNotReceive().GetNextSequenceAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Generate_ShouldRejectOrderNotPaid(OrderStatus status)
    {
        if (status == OrderStatus.Confirmed) Assert.True(order.Confirm().IsSuccess);
        if (status == OrderStatus.Cancelled) order.Cancel("Cancelled");
        Assert.Equal(status, order.Status);
        Assert.Equal(InvoiceErrors.OrderNotPaid, (await Generate()).Error);
        await invoices.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_ShouldRejectAlreadyInvoicedOrder()
    {
        MarkPaid();
        invoices.ExistsForOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(true);
        Assert.Equal(InvoiceErrors.AlreadyInvoiced, (await Generate()).Error);
        await invoices.DidNotReceive().GetNextSequenceAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_ShouldRejectMissingStore()
    {
        MarkPaid();
        stores.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns((Store?)null);
        Assert.Equal(StoreErrors.StoreNotFound, (await Generate()).Error);
        await invoices.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Asia/Ho_Chi_Minh")]
    [InlineData("Pacific/Kiritimati")]
    [InlineData("Etc/GMT+12")]
    [InlineData("Unknown/InvoiceTimezone")]
    [InlineData("Invalid\0Timezone")]
    public async Task Generate_ShouldUseStoreLocalDateOrUtcFallback_AndFormatSequence(string timezoneId)
    {
        MarkPaid();
        store.UpdateInfo(store.Name, null, null, timezoneId, "VND", null, null, null);
        var fallback = timezoneId.StartsWith("Unknown") || timezoneId.StartsWith("Invalid");
        var timezone = fallback ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        var before = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone).Date);
        Assert.True((await Generate()).IsSuccess);
        var after = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone).Date);
        await invoices.Received(1).GetNextSequenceAsync(store.Id,
            Arg.Is<DateOnly>(d => d == before || d == after), Arg.Any<CancellationToken>());
        await invoices.Received(1).AddAsync(Arg.Is<Invoice>(i => i.OrderId == order.Id &&
            i.TotalBeforeTax == order.Subtotal && i.TaxAmount == order.TaxTotal && i.GrandTotal == order.GrandTotal &&
            (i.InvoiceNo == $"HD-{store.Code}-{before:yyyyMMdd}-000042" ||
             i.InvoiceNo == $"HD-{store.Code}-{after:yyyyMMdd}-000042")), Arg.Any<CancellationToken>());
        Assert.Equal(fallback ? 1 : 0, logger.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "Log" &&
            c.GetArguments()[0] is LogLevel.Warning));
    }

    [Fact]
    public async Task Generate_Pipeline_ShouldValidateOrderId_AndNotRequirePermission()
    {
        Assert.False(typeof(IRequirePermission).IsAssignableFrom(typeof(GenerateInvoiceCommand)));
        var validation = new ValidationBehavior<GenerateInvoiceCommand, Result>([new GenerateInvoiceCommandValidator()]);
        var invalid = await validation.Handle(new(Guid.Empty), () => throw new Exception("Handler must not run"), default);
        Assert.True(invalid.IsFailure);

        var authorization = new AuthorizationBehavior<GenerateInvoiceCommand, Result>(
            Substitute.For<ICurrentUser>(), Substitute.For<IServiceProvider>());
        Assert.True((await authorization.Handle(new(order.Id), () => Task.FromResult(Result.Success()), default)).IsSuccess);
    }
}
