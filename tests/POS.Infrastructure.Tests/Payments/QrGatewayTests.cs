using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Infrastructure.Payments;

namespace POS.Infrastructure.Tests.Payments;

public class QrGatewayTests
{
    private const string Secret = "unit-test-secret-only";
    private static readonly MomoOptions Momo = new()
    {
        PartnerCode = "TEST", AccessKey = "test-access", SecretKey = Secret,
        IpnUrl = "https://merchant.example/ipn", RedirectUrl = "https://merchant.example/result"
    };

    private static MomoWebhookDto Payload() => new("TEST", "merchant-id", "merchant-id", 180000,
        "Order", "momo_wallet", 1234567, 0, "Successful.", "qr", 1700000000000,
        Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"orderId\":\"11111111-1111-1111-1111-111111111111\"}")), "");

    private static string IndependentSignature(MomoWebhookDto p) => Convert.ToHexStringLower(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(
            $"accessKey=test-access&amount={p.Amount}&extraData={p.ExtraData}&message={p.Message}&orderId={p.OrderId}&orderInfo={p.OrderInfo}&orderType={p.OrderType}&partnerCode={p.PartnerCode}&payType={p.PayType}&requestId={p.RequestId}&responseTime={p.ResponseTime}&resultCode={p.ResultCode}&transId={p.TransId}")));

    [Fact]
    public void Momo_VerifiesOfficialFieldOrder_AndExtractsSignedOrder()
    {
        var payload = Payload();
        payload = payload with { Signature = IndependentSignature(payload) };
        var verifier = new MomoWebhookSignatureVerifier(Options.Create(Momo));
        Assert.True(verifier.Verify(new(Momo: payload)));
        var adapter = new MoMoPaymentAdapter(new HttpClient(), Options.Create(Momo), [verifier]);
        var result = adapter.VerifyWebhook(new(Momo: payload));
        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result.Value!.OrderId);
        Assert.Equal("1234567", result.Value.TransactionReference);
        Assert.Equal(TimeSpan.FromMinutes(5), result.Value.TimestampTolerance);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong")]
    [InlineData("malformed")]
    [InlineData("amount")]
    [InlineData("message")]
    [InlineData("order")]
    public void Momo_RejectsMissingInvalidOrTamperedSignature(string kind)
    {
        var payload = Payload();
        payload = payload with { Signature = IndependentSignature(payload) };
        payload = kind switch
        {
            "missing" => payload with { Signature = "" },
            "wrong" => payload with { Signature = new string('0', 64) },
            "malformed" => payload with { Signature = new string('x', 64) },
            "amount" => payload with { Amount = payload.Amount + 1 },
            "message" => payload with { Message = "tampered" },
            _ => payload with { OrderId = "another-order" }
        };
        Assert.False(new MomoWebhookSignatureVerifier(Options.Create(Momo)).Verify(new(Momo: payload)));
    }

    [Theory]
    [InlineData(0, true, true)]
    [InlineData(9000, true, true)]
    [InlineData(1006, false, true)]
    [InlineData(1000, false, false)]
    [InlineData(7000, false, false)]
    [InlineData(7002, false, false)]
    [InlineData(8000, false, false)]
    public void Momo_MapsResultCodesForAutoCapture(int code, bool success, bool final)
    {
        var p = Payload() with { ResultCode = code };
        p = p with { Signature = IndependentSignature(p) };
        var verifier = new MomoWebhookSignatureVerifier(Options.Create(Momo));
        var result = new MoMoPaymentAdapter(new HttpClient(), Options.Create(Momo), [verifier]).VerifyWebhook(new(Momo: p));
        Assert.True(result.IsSuccess);
        Assert.Equal(success, result.Value!.Success);
        Assert.Equal(final, result.Value.IsFinal);
    }

    [Theory]
    [InlineData("", 65535)]
    [InlineData("123456789", 10673)] // CRC-16/CCITT-FALSE check = 0x29B1.
    public void Crc16_StandardCheckVectors(string value, int expected) => Assert.Equal(expected, VietQrPayload.Crc16(value));

    [Fact]
    public void VietQr_FixedDynamicPayload_HasNestedBeneficiaryAndIndependentCrc()
    {
        // Expected CRC C935 calculated independently with Python binascii.crc_hqx(data, 0xFFFF).
        const string expected = "00020101021238570010A00000072701270006970403011300110123456780208QRIBFTTA530370454061800005802VN62270823POS0123456789ABCDEF01236304C935";
        var result = VietQrPayload.Build("970403", "0011012345678", 180000, "POS0123456789ABCDEF0123");
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("97040", "123", 100, "POS123")]
    [InlineData("970403", "123", 100.5, "POS123")]
    [InlineData("970403", "123", 0, "POS123")]
    [InlineData("970403", "123", 100, "không dấu")]
    public void VietQr_RejectsInvalidData(string bin, string account, decimal amount, string memo) =>
        Assert.True(VietQrPayload.Build(bin, account, amount, memo).IsFailure);

    [Fact]
    public void VietQr_RawBytesMatter_AndOutboundTransfersAreRejected()
    {
        var o = Options.Create(new VietQrOptions { BankBin = "970403", AccountNo = "12345", WebhookSecret = Secret });
        var payload = new VietQrWebhookDto("Bank", "2026-10-22", "12345", null, 180000,
            "POS0123456789ABCDEF0123", "in", null, 5678, null);
        var raw = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), raw));
        var verifier = new VietQrWebhookSignatureVerifier(o);
        var adapter = new VietQRAdapter(o, [verifier], TimeProvider.System);
        var envelope = new WebhookEnvelopeDto(VietQr: payload, RawBody: raw, Signature: signature);
        Assert.True(adapter.VerifyWebhook(envelope).IsSuccess);
        Assert.False(verifier.Verify(envelope with { Signature = null }));
        Assert.False(verifier.Verify(envelope with { RawBody = [.. raw, (byte)' '] }));
        Assert.True(adapter.VerifyWebhook(envelope with { VietQr = payload with { Amount = 1 } }).IsFailure);
        var outgoing = payload with { TransferType = "out" };
        var outgoingRaw = JsonSerializer.SerializeToUtf8Bytes(outgoing, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.True(adapter.VerifyWebhook(new(VietQr: outgoing, RawBody: outgoingRaw,
            Signature: Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), outgoingRaw)))).IsFailure);
    }

    [Fact]
    public async Task Momo_CreateRequestIsSignedWithStableIds_AndUncertaintyStaysPending()
    {
        var payment = Payment.CreatePendingQr(Guid.NewGuid(), PaymentMethod.MoMo, 180000,
            "POS0123456789ABCDEF0123", "{}");
        var session = new QrSessionDto("", null, DateTimeOffset.UtcNow.AddMinutes(15));
        var http = new HttpClient(new CallbackHandler(async request =>
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var root = json.RootElement;
            var orderId = root.GetProperty("orderId").GetString();
            Assert.Equal(payment.MerchantReference, orderId);
            Assert.Equal(orderId, root.GetProperty("requestId").GetString());
            var canonical = $"accessKey=test-access&amount=180000&extraData={root.GetProperty("extraData").GetString()}&ipnUrl=https://merchant.example/ipn&orderId={orderId}&orderInfo=POS {payment.OrderId:N}&partnerCode=TEST&redirectUrl=https://merchant.example/result&requestId={orderId}&requestType=captureWallet";
            Assert.Equal(Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(canonical))),
                root.GetProperty("signature").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { resultCode = 0, orderId, requestId = orderId,
                    amount = 180000, payUrl = "https://payment.example/pay" }), Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri(Momo.ApiBaseUrl) };
        var adapter = new MoMoPaymentAdapter(http, Options.Create(Momo), []);
        var result = await adapter.GenerateAsync(payment, session, default);
        Assert.True(result.IsSuccess);
        Assert.Equal("https://payment.example/pay", result.Value!.QrCodeData);
        Assert.Equal(session.ExpiresAt, result.Value.ExpiresAt);
        var failure = new MoMoPaymentAdapter(new HttpClient(new CallbackHandler(_ => throw new HttpRequestException()))
            { BaseAddress = new Uri(Momo.ApiBaseUrl) }, Options.Create(Momo), []);
        Assert.True((await failure.GenerateAsync(payment, session, default)).IsFailure);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    private sealed class CallbackHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
