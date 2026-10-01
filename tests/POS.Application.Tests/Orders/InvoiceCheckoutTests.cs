using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.DTOs;
using POS.Domain.Common;
using POS.Domain.Orders;

namespace POS.Application.Tests.Orders;

public partial class CheckoutAndCancelOrderTests
{
    [Fact]
    public void Checkout_ShouldDependOnSender_WithoutInvoiceRepository()
    {
        var dependencies = typeof(CheckoutOrderCommandHandler).GetConstructors()
            .SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).ToList();
        Assert.Contains(typeof(ISender), dependencies);
        Assert.DoesNotContain(typeof(IInvoiceRepository), dependencies);
    }

    [Fact]
    public async Task Checkout_ShouldSendGenerateInvoiceInsideTransaction_OnlyWhenPaid()
    {
        SetUpPaymentOrder(100);
        var inTransaction = false;
        using var cancellation = new CancellationTokenSource();
        _unitOfWork.ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<Result<CheckoutDto>>>>(),
            Arg.Any<CancellationToken>()).Returns(async call =>
        {
            inTransaction = true;
            try { return await call.Arg<Func<CancellationToken, Task<Result<CheckoutDto>>>>()(cancellation.Token); }
            finally { inTransaction = false; }
        });
        _sender.Send(Arg.Any<GenerateInvoiceCommand>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Assert.True(inTransaction);
            Assert.Equal(cancellation.Token, call.Arg<CancellationToken>());
            Assert.Equal(_orderId, call.Arg<GenerateInvoiceCommand>().OrderId);
            return Result.Success();
        });

        var handler = CreateCheckoutHandler();
        Assert.True((await handler.Handle(new(_orderId, [new("Cash", 40)]), default)).IsSuccess);
        await _sender.DidNotReceive().Send(Arg.Any<GenerateInvoiceCommand>(), Arg.Any<CancellationToken>());
        Assert.True((await handler.Handle(new(_orderId, [new("Cash", 60)]), default)).IsSuccess);
        await _sender.Received(1).Send(Arg.Any<GenerateInvoiceCommand>(), cancellation.Token);
    }

    [Fact]
    public async Task Checkout_ShouldPropagateInvoiceFailure_WithoutSaving()
    {
        SetUpPaymentOrder(100);
        _sender.Send(Arg.Any<GenerateInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(InvoiceErrors.AlreadyInvoiced));
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 100)]), default);
        Assert.Equal(InvoiceErrors.AlreadyInvoiced, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Checkout_ShouldPropagateInvoiceException_WithoutSaving()
    {
        SetUpPaymentOrder(100);
        _sender.Send(Arg.Any<GenerateInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns<Result>(_ => throw new InvalidOperationException("Invoice failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 100)]), default));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Checkout_ShouldSucceedWithUtcInvoice_WhenStoreTimezoneIsUnknown()
    {
        SetUpPaymentOrder(100);
        _store.UpdateInfo(_store.Name, null, null, "Unknown/InvoiceTimezone", "VND", null, null, null);
        var before = DateTime.UtcNow.ToString("yyyyMMdd");
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 100)]), default);
        var after = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.True(result.IsSuccess);
        await _invoiceRepository.Received(1).AddAsync(Arg.Is<Invoice>(i =>
            i.InvoiceNo == $"HD-{_store.Code}-{before}-000001" ||
            i.InvoiceNo == $"HD-{_store.Code}-{after}-000001"), Arg.Any<CancellationToken>());
    }
}
