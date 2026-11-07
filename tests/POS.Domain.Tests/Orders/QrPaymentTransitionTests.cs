using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Domain.Tests.Orders;

public class QrPaymentTransitionTests
{
    [Theory]
    [InlineData(PaymentStatus.Success)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Timeout)]
    public void FinalPaymentCannotBeRewritten(PaymentStatus status)
    {
        var payment = Payment.CreatePendingQr(Guid.NewGuid(), PaymentMethod.MoMo, 100, "POS123", "{}");
        if (status == PaymentStatus.Timeout) payment.ExpireQr();
        else payment.CompleteQr("provider-ref", status == PaymentStatus.Success, DateTime.UtcNow);
        var paidAt = payment.PaidAt;
        Assert.True(payment.CompleteQr("other-ref", true, DateTime.UtcNow).IsFailure);
        Assert.True(payment.SetQrMetadata("{\"changed\":true}").IsFailure);
        Assert.True(payment.ExpireQr().IsFailure);
        Assert.True(payment.MarkSuccess().IsFailure);
        Assert.True(payment.MarkFailed().IsFailure);
        Assert.Equal(status, payment.Status);
        Assert.Equal(paidAt, payment.PaidAt);
    }
}
