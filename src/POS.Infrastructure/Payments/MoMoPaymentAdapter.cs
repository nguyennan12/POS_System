using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;
using PaymentEntity = POS.Domain.Orders.Payment;

namespace POS.Infrastructure.Payments;

public sealed class MoMoPaymentAdapter(HttpClient http, IOptions<MomoOptions> options,
    IEnumerable<IWebhookSignatureVerifier> verifiers) : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.MoMo;

    public async Task<Result<QrSessionDto>> GenerateAsync(PaymentEntity payment, QrSessionDto session, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.PartnerCode) || string.IsNullOrWhiteSpace(o.AccessKey)
            || string.IsNullOrWhiteSpace(o.SecretKey) || !IsHttps(o.IpnUrl) || !IsHttps(o.RedirectUrl)
            || payment.MerchantReference is null || payment.Amount <= 0 || payment.Amount != decimal.Truncate(payment.Amount))
            return PaymentErrors.GatewayUnavailable;
        var reference = payment.MerchantReference;
        var extraData = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new { orderId = payment.OrderId }));
        var orderInfo = "POS " + payment.OrderId.ToString("N");
        const string requestType = "captureWallet";
        var amount = payment.Amount.ToString("0", CultureInfo.InvariantCulture);
        var signingData = $"accessKey={o.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={o.IpnUrl}&orderId={reference}&orderInfo={orderInfo}&partnerCode={o.PartnerCode}&redirectUrl={o.RedirectUrl}&requestId={reference}&requestType={requestType}";
        var request = new
        {
            partnerCode = o.PartnerCode, requestId = reference, orderId = reference,
            amount = (long)payment.Amount, orderInfo, redirectUrl = o.RedirectUrl, ipnUrl = o.IpnUrl,
            requestType, extraData, lang = "vi", autoCapture = true,
            signature = PaymentHmac.Sign(o.SecretKey, signingData)
        };
        try
        {
            using var response = await http.PostAsJsonAsync("v2/gateway/api/create", request, ct);
            if (!response.IsSuccessStatusCode) return PaymentErrors.GatewayUnavailable;
            var created = await response.Content.ReadFromJsonAsync<CreateResponse>(cancellationToken: ct);
            if (created is null || created.ResultCode != 0 || created.OrderId != reference
                || created.RequestId != reference || created.Amount != (long)payment.Amount
                || (string.IsNullOrWhiteSpace(created.QrCodeUrl) && string.IsNullOrWhiteSpace(created.PayUrl)))
                return PaymentErrors.GatewayUnavailable;
            return session with
            {
                QrCodeData = string.IsNullOrWhiteSpace(created.QrCodeUrl) ? created.PayUrl! : created.QrCodeUrl,
                DeepLink = created.Deeplink, ProviderRef = created.OrderId
            };
        }
        catch (HttpRequestException) { return PaymentErrors.GatewayUnavailable; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return PaymentErrors.GatewayUnavailable; }
        catch (JsonException) { return PaymentErrors.GatewayUnavailable; }
    }

    public Result<VerifiedWebhookDto> VerifyWebhook(WebhookEnvelopeDto webhook)
    {
        var p = webhook.Momo;
        var o = options.Value;
        if (p is null || !verifiers.Single(x => x.Method == Method).Verify(webhook)
            || p.PartnerCode != o.PartnerCode || p.OrderId != p.RequestId || p.Amount <= 0 || p.TransId <= 0
            || o.ReplayToleranceSeconds <= 0)
            return PaymentErrors.InvalidWebhook;
        try
        {
            using var extra = JsonDocument.Parse(Convert.FromBase64String(p.ExtraData));
            if (!extra.RootElement.TryGetProperty("orderId", out var value) || !value.TryGetGuid(out var orderId))
                return PaymentErrors.InvalidWebhook;
            return new VerifiedWebhookDto(p.OrderId, p.TransId.ToString(CultureInfo.InvariantCulture), p.Amount,
                p.ResultCode is 0 or 9000, DateTimeOffset.FromUnixTimeMilliseconds(p.ResponseTime), orderId,
                TimeSpan.FromSeconds(o.ReplayToleranceSeconds), p.ResultCode is not (1000 or 7000 or 7002 or 8000));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return PaymentErrors.InvalidWebhook;
        }
    }

    private static bool IsHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https";

    private sealed record CreateResponse(int ResultCode, string? OrderId, string? RequestId, long Amount,
        string? QrCodeUrl, string? PayUrl, string? Deeplink);
}
