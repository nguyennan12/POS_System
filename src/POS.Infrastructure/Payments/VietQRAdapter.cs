using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;
using PaymentEntity = POS.Domain.Orders.Payment;

namespace POS.Infrastructure.Payments;

public sealed partial class VietQRAdapter(IOptions<VietQrOptions> options, IEnumerable<IWebhookSignatureVerifier> verifiers,
    TimeProvider clock) : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.VietQR;

    public Task<Result<QrSessionDto>> GenerateAsync(PaymentEntity payment, QrSessionDto session, CancellationToken ct)
    {
        var payload = VietQrPayload.Build(options.Value.BankBin, options.Value.AccountNo,
            payment.Amount, payment.MerchantReference ?? "");
        return Task.FromResult(payload.IsFailure ? Result<QrSessionDto>.Failure(payload.Error)
            : Result<QrSessionDto>.Success(session with { QrCodeData = payload.Value!, ProviderRef = payment.MerchantReference }));
    }

    public Result<VerifiedWebhookDto> VerifyWebhook(WebhookEnvelopeDto webhook)
    {
        if (!verifiers.Single(x => x.Method == Method).Verify(webhook) || webhook.VietQr is not { } p
            || p.AccountNumber != options.Value.AccountNo || p.TransferType != "in" || p.Amount <= 0
            || p.TransactionId <= 0 || string.IsNullOrWhiteSpace(p.Content)
            || (options.Value.ExpectedGateway is { Length: > 0 } expected && p.Gateway != expected))
            return PaymentErrors.InvalidWebhook;
        // Bind the interpreted fields to the actual signed bytes, even for non-HTTP callers.
        try
        {
            if (JsonSerializer.Deserialize<VietQrWebhookDto>(webhook.RawBody!, new JsonSerializerOptions(JsonSerializerDefaults.Web)) != p)
                return PaymentErrors.InvalidWebhook;
        }
        catch (JsonException) { return PaymentErrors.InvalidWebhook; }
        var matches = MerchantReferencePattern().Matches(p.Content);
        if (matches.Count != 1) return PaymentErrors.InvalidWebhook;
        return new VerifiedWebhookDto(matches[0].Value, p.TransactionId.ToString(CultureInfo.InvariantCulture),
            p.Amount, true, clock.GetUtcNow());
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9])POS[A-F0-9]{20}(?![A-Za-z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex MerchantReferencePattern();
}
