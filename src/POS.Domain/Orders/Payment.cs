using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Domain.Orders;

public class Payment : BaseEntity
{
    public Payment() : base()
    {
    }

    public Payment(
        Guid orderId,
        PaymentMethod method,
        decimal amount,
        string? transactionRef = null,
        PaymentStatus status = PaymentStatus.Success,
        decimal? changeAmount = null,
        string? gatewayResponseJson = null,
        DateTime? paidAt = null,
        Guid? id = null) : base(id)
    {
        OrderId = orderId;
        Method = method;
        Amount = amount;
        TransactionRef = transactionRef;
        Status = status;
        ChangeAmount = changeAmount;
        GatewayResponseJson = gatewayResponseJson;
        PaidAt = paidAt ?? (status == PaymentStatus.Success ? DateTime.UtcNow : null);
    }

    public static Payment CreateSuccess(
        Guid orderId,
        PaymentMethod method,
        decimal amount,
        string? transactionRef = null,
        decimal? changeAmount = null,
        Guid? id = null)
    {
        return new Payment(
            orderId,
            method,
            amount,
            transactionRef,
            status: PaymentStatus.Success,
            changeAmount: changeAmount,
            paidAt: DateTime.UtcNow,
            id: id);
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;

    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? ChangeAmount { get; private set; }
    public string? TransactionRef { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? GatewayResponseJson { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public void SetChangeAmount(decimal? changeAmount)
    {
        ChangeAmount = changeAmount;
    }

    public void MarkSuccess(DateTime? paidAt = null)
    {
        Status = PaymentStatus.Success;
        PaidAt = paidAt ?? DateTime.UtcNow;
    }

    public void MarkFailed(string? gatewayResponseJson = null)
    {
        Status = PaymentStatus.Failed;
        GatewayResponseJson = gatewayResponseJson;
    }
}
