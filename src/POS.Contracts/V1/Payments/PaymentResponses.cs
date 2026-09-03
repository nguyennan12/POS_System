namespace POS.Contracts.V1.Payments;

public record QrPaymentResponse(
    Guid PaymentId,
    Guid OrderId,
    string Method,
    decimal Amount,
    string QrCodeData,
    string? DeepLink,
    DateTimeOffset ExpiresAt
);

public record PaymentStatusResponse(
    Guid PaymentId,
    Guid OrderId,
    string Method,
    decimal Amount,
    string Status,
    string? TransactionRef,
    DateTimeOffset? PaidAt
);

public record MomoWebhookResponse(
    int ResultCode,
    string Message
);

public record VietQrWebhookResponse(
    bool Success,
    string Message
);
