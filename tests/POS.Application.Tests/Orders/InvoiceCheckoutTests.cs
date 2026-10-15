using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.DTOs;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Events;

namespace POS.Application.Tests.Orders;

public partial class CheckoutAndCancelOrderTests
{
    [Fact]
    public void Checkout_ShouldDependOnPublisher_WithoutISenderOrInvoiceRepository()
    {
        var dependencies = typeof(CheckoutOrderCommandHandler).GetConstructors()
            .SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).ToList();
        Assert.Contains(typeof(IPublisher), dependencies);
        Assert.DoesNotContain(typeof(IInvoiceRepository), dependencies);
        Assert.DoesNotContain(typeof(ISender), dependencies);
    }

    [Fact]
    public async Task Checkout_ShouldPublishOrderPaidDomainEvent_InsideTransaction_OnlyWhenPaid()
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
        _publisher.Publish(Arg.Any<OrderPaidDomainEvent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Assert.True(inTransaction);
            Assert.Equal(cancellation.Token, call.Arg<CancellationToken>());
            Assert.Equal(_orderId, call.Arg<OrderPaidDomainEvent>().OrderId);
            return Task.CompletedTask;
        });

        var handler = CreateCheckoutHandler();
        Assert.True((await handler.Handle(new(_orderId, [new("Cash", 40)]), default)).IsSuccess);
        await _publisher.DidNotReceive().Publish(Arg.Any<OrderPaidDomainEvent>(), Arg.Any<CancellationToken>());
        Assert.True((await handler.Handle(new(_orderId, [new("Cash", 60)]), default)).IsSuccess);
        await _publisher.Received(1).Publish(Arg.Any<OrderPaidDomainEvent>(), cancellation.Token);
    }

    [Fact]
    public async Task Checkout_ShouldPropagatePublisherException_WithoutSaving()
    {
        SetUpPaymentOrder(100);
        _publisher.Publish(Arg.Any<OrderPaidDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Invoice handler failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 100)]), default));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
