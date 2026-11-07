using System.Text.Json;
using POS.Domain.Orders;

namespace POS.Application.UseCases.Payments.DTOs;

public record QrPaymentDto(Guid PaymentId, Guid OrderId, string Method, decimal Amount,
    string QrCodeData, string? DeepLink, DateTimeOffset ExpiresAt);

public record PaymentStatusDto(Guid PaymentId, Guid OrderId, string Method, decimal Amount,
    string Status, string? TransactionRef, DateTimeOffset? PaidAt)
{
    public static PaymentStatusDto From(Payment payment) => new(payment.Id, payment.OrderId,
        payment.Method.ToString(), payment.Amount, payment.Status.ToString(), payment.TransactionRef,
        payment.PaidAt is { } at ? new DateTimeOffset(DateTime.SpecifyKind(at, DateTimeKind.Utc)) : null);
}

// Only whitelisted gateway fields are persisted. Never store webhook bodies or signatures here.
public record QrSessionDto(string QrCodeData, string? DeepLink, DateTimeOffset ExpiresAt,
    string? ProviderRef = null)
{
    public string ToJson() => JsonSerializer.Serialize(this);

    public static QrSessionDto? Read(Payment payment)
    {
        if (payment.MerchantReference is null || payment.GatewayResponseJson is null) return null;
        try { return JsonSerializer.Deserialize<QrSessionDto>(payment.GatewayResponseJson); }
        catch (JsonException) { return null; }
    }

    public QrPaymentDto ToDto(Payment payment) => new(payment.Id, payment.OrderId,
        payment.Method.ToString(), payment.Amount, QrCodeData, DeepLink, ExpiresAt);
}

public record MomoWebhookDto(string PartnerCode, string OrderId, string RequestId, long Amount,
    string OrderInfo, string OrderType, long TransId, int ResultCode, string Message, string PayType,
    long ResponseTime, string ExtraData, string Signature);

public record VietQrWebhookDto(string Gateway, string TransactionDate, string AccountNumber,
    string? SubAccount, decimal Amount, string Content, string? TransferType, string? Description,
    long TransactionId, string? ReferenceCode);

public record WebhookEnvelopeDto(MomoWebhookDto? Momo = null, VietQrWebhookDto? VietQr = null,
    byte[]? RawBody = null, string? Signature = null);

public record VerifiedWebhookDto(string MerchantReference, string TransactionReference,
    decimal Amount, bool Success, DateTimeOffset OccurredAt, Guid? OrderId = null,
    TimeSpan? TimestampTolerance = null, bool IsFinal = true);

public record WebhookAckDto(bool Success = true);
public record PaymentRealtimeDto(Guid StoreId, PaymentStatusDto Payment);

public sealed class QrPaymentOptions
{
    public int LifetimeMinutes { get; set; } = 15;
}
