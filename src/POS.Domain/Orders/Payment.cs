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
    public string? MerchantReference { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? GatewayResponseJson { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public static Payment CreatePendingQr(Guid orderId, PaymentMethod method, decimal amount,
        string merchantReference, string metadata)
    {
        return new Payment(orderId, method, amount, status: PaymentStatus.Pending,
            gatewayResponseJson: metadata) { MerchantReference = merchantReference };
    }

    public Result SetQrMetadata(string metadata)
    {
        if (Status != PaymentStatus.Pending)
            return new Error(ErrorType.Invalid, "PAYMENT.FINAL_STATE", "Thanh toán đã kết thúc.");
        GatewayResponseJson = metadata;
        return Result.Success();
    }

    public Result CompleteQr(string transactionRef, bool success, DateTime paidAt)
    {
        if (Status != PaymentStatus.Pending || string.IsNullOrWhiteSpace(transactionRef))
            return new Error(ErrorType.Invalid, "PAYMENT.INVALID_TRANSITION", "Không thể cập nhật thanh toán.");
        TransactionRef = transactionRef;
        Status = success ? PaymentStatus.Success : PaymentStatus.Failed;
        PaidAt = success ? paidAt : null;
        return Result.Success();
    }

    public Result ExpireQr()
    {
        if (Status != PaymentStatus.Pending)
            return new Error(ErrorType.Invalid, "PAYMENT.FINAL_STATE", "Thanh toán đã kết thúc.");
        Status = PaymentStatus.Timeout;
        return Result.Success();
    }

    public void SetChangeAmount(decimal? changeAmount)
    {
        ChangeAmount = changeAmount;
    }

    public Result MarkSuccess(DateTime? paidAt = null)
    {
        if (Status != PaymentStatus.Pending)
            return new Error(ErrorType.Invalid, "PAYMENT.FINAL_STATE", "Thanh toán đã kết thúc.");
        Status = PaymentStatus.Success;
        PaidAt = paidAt ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkFailed(string? gatewayResponseJson = null)
    {
        if (Status != PaymentStatus.Pending)
            return new Error(ErrorType.Invalid, "PAYMENT.FINAL_STATE", "Thanh toán đã kết thúc.");
        Status = PaymentStatus.Failed;
        GatewayResponseJson = gatewayResponseJson;
        return Result.Success();
    }
}
