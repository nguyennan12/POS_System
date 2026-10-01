using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Promotions.Services.Models;

namespace POS.Domain.Tests.Orders;

public class OrderPaymentTests
{
    private static Order ConfirmedOrder()
    {
        var order = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sku = new Sku(Guid.NewGuid(), order.StoreId, "SKU", "123456789", 100, 50, 0, true);
        order.AddOrUpdateItem(sku, 1);
        order.ApplyPromotionEvaluation(new PromotionResult(100, 0, 100, [], [], []), new Dictionary<Guid, decimal>());
        order.Confirm();
        return order;
    }

    [Fact]
    public void ProcessPayments_ShouldNotMutateOrder_WhenLaterSplitIsInvalid()
    {
        var order = ConfirmedOrder();
        Assert.Throws<InvalidOperationException>(() => order.ProcessPayments(
            [(PaymentMethod.Cash, 80, null), (PaymentMethod.Card, 30, "ref")]));
        Assert.Empty(order.Payments);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void AggregatePaymentStatus_ShouldCountOnlySuccess_AndKeepPaidTimestampStable()
    {
        var order = ConfirmedOrder();
        order.ProcessPayments([(PaymentMethod.Cash, 40, null)]);
        var pending = new Payment(order.Id, PaymentMethod.Card, 60, "ref", PaymentStatus.Pending);
        order.Payments.Add(pending);
        order.Payments.Add(new Payment(order.Id, PaymentMethod.MoMo, 100, "failed", PaymentStatus.Failed));
        order.Payments.Add(new Payment(order.Id, PaymentMethod.VietQR, 100, "timeout", PaymentStatus.Timeout));
        order.AggregatePaymentStatus();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(40, order.GetPaymentTotals().TotalApplied);
        Assert.Null(order.PaidAt);

        pending.MarkSuccess();
        order.AggregatePaymentStatus();
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(100, order.GetPaymentTotals().TotalApplied);
        var paidAt = order.PaidAt;
        order.AggregatePaymentStatus();
        Assert.Equal(paidAt, order.PaidAt);
    }

    [Fact]
    public void GetPaymentTotals_ShouldSubtractCashChange()
    {
        var order = ConfirmedOrder();
        order.ProcessPayments([(PaymentMethod.Card, 60, "ref")]);
        var result = order.ProcessPayments([(PaymentMethod.Cash, 50, null)]);
        Assert.Equal(100, result.TotalApplied);
        Assert.Equal(10, result.ChangeAmount);
        Assert.Equal(OrderStatus.Paid, result.Status);
        Assert.Equal(50, order.Payments.Last().Amount);
    }

    [Fact]
    public void AggregatePaymentStatus_ShouldNotReviveCancelledOrder()
    {
        var order = ConfirmedOrder();
        order.Cancel();
        order.Payments.Add(Payment.CreateSuccess(order.Id, PaymentMethod.Cash, 100));
        order.AggregatePaymentStatus();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Null(order.PaidAt);
    }
}
