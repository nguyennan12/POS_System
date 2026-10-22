using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Orders.Enums;

namespace POS.Infrastructure.Payments;

public static class PaymentHmac
{
    public static string Sign(string secret, string value) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(value)));

    public static bool Verify(string secret, ReadOnlySpan<byte> body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(secret) || signature is null || signature.Length != 64) return false;
        byte[] supplied;
        try { supplied = Convert.FromHexString(signature); }
        catch (FormatException) { return false; }
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }
}

public sealed class MomoWebhookSignatureVerifier(IOptions<MomoOptions> options) : IWebhookSignatureVerifier
{
    public PaymentMethod Method => PaymentMethod.MoMo;

    // MoMo v2 captureWallet IPN signing fields, ordered exactly as documented:
    // https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/
    // This is deliberately different from the create-request signing string.
    public static string SigningData(string accessKey, MomoWebhookDto p) => string.Create(CultureInfo.InvariantCulture,
        $"accessKey={accessKey}&amount={p.Amount}&extraData={p.ExtraData}&message={p.Message}&orderId={p.OrderId}&orderInfo={p.OrderInfo}&orderType={p.OrderType}&partnerCode={p.PartnerCode}&payType={p.PayType}&requestId={p.RequestId}&responseTime={p.ResponseTime}&resultCode={p.ResultCode}&transId={p.TransId}");

    public bool Verify(WebhookEnvelopeDto webhook) => webhook.Momo is { } p
        && !string.IsNullOrWhiteSpace(options.Value.AccessKey)
        && PaymentHmac.Verify(options.Value.SecretKey, Encoding.UTF8.GetBytes(SigningData(options.Value.AccessKey, p)), p.Signature);
}

// Explicit temporary integration scheme; replace this verifier for the selected bank/provider.
public sealed class VietQrWebhookSignatureVerifier(IOptions<VietQrOptions> options) : IWebhookSignatureVerifier
{
    public PaymentMethod Method => PaymentMethod.VietQR;
    public bool Verify(WebhookEnvelopeDto webhook) => webhook.RawBody is { Length: > 0 } body
        && PaymentHmac.Verify(options.Value.WebhookSecret, body, webhook.Signature);
}
