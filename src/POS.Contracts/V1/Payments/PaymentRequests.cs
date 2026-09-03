namespace POS.Contracts.V1.Payments;

public record GenerateQrPaymentRequest(
    Guid OrderId,
    string Method
);

public record MomoWebhookRequest(
    string PartnerCode,
    string OrderId,
    string RequestId,
    long Amount,
    string OrderInfo,
    string OrderType,
    long TransId,
    int ResultCode,
    string Message,
    string PayType,
    long ResponseTime,
    string ExtraData,
    string Signature
);

public record VietQrWebhookRequest(
    string Gateway,
    string TransactionDate,
    string AccountNumber,
    string? SubAccount,
    decimal Amount,
    string Content,
    string? TransferType,
    string? Description,
    long TransactionId,
    string? ReferenceCode
);
