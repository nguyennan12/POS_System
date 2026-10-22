using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using POS.Api.Controllers;
using POS.Api.Extensions;
using POS.Application.UseCases.Payments.Commands.GenerateQrPayment;
using POS.Application.UseCases.Payments.Commands.ProcessMomoWebhook;
using POS.Application.UseCases.Payments.Commands.ProcessVietQrWebhook;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;

namespace POS.Api.Tests.Payments;

public class QrPaymentsControllerTests
{
    [Fact]
    public async Task Generate_MapsRequestAndApplicationDto_WithoutChangingContract()
    {
        var sender = Substitute.For<ISender>();
        var orderId = Guid.NewGuid();
        var dto = new QrPaymentDto(Guid.NewGuid(), orderId, "VietQR", 180000, "payload", null, DateTimeOffset.UtcNow.AddMinutes(15));
        sender.Send(Arg.Is<GenerateQrPaymentCommand>(c => c.OrderId == orderId && c.Method == "VietQR"), Arg.Any<CancellationToken>())
            .Returns(Result<QrPaymentDto>.Success(dto));
        var response = await new PaymentsController(sender).GenerateQr(new(orderId, "VietQR"), default);
        var envelope = Assert.IsType<ApiResponse<QrPaymentResponse>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(dto.PaymentId, envelope.Data!.PaymentId);
        Assert.Equal(dto.QrCodeData, envelope.Data.QrCodeData);
        Assert.Equal(dto.ExpiresAt, envelope.Data.ExpiresAt);
    }

    [Fact]
    public async Task Momo_AcknowledgesWith204_AndHidesFailureDetails()
    {
        var sender = Substitute.For<ISender>();
        var request = new MomoWebhookRequest("TEST", "ref", "ref", 180000, "Order", "momo_wallet", 123, 0,
            "Successful", "qr", 1700000000000, "", new string('a', 64));
        sender.Send(Arg.Any<ProcessMomoWebhookCommand>(), Arg.Any<CancellationToken>()).Returns(Result<WebhookAckDto>.Success(new()));
        Assert.IsType<NoContentResult>(await new PaymentsController(sender).MomoWebhook(request, default));
        sender.Send(Arg.Any<ProcessMomoWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<WebhookAckDto>.Failure(new Error(ErrorType.Invalid, "DB.INTERNAL", "sensitive details")));
        var failure = Assert.IsType<BadRequestObjectResult>(await new PaymentsController(sender).MomoWebhook(request, default));
        Assert.DoesNotContain("sensitive", Assert.IsType<MomoWebhookResponse>(failure.Value).Message);
    }

    [Fact]
    public void OnlyWebhooksAllowAnonymous()
    {
        Assert.NotEmpty(typeof(PaymentsController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        foreach (var name in new[] { nameof(PaymentsController.MomoWebhook), nameof(PaymentsController.VietQrWebhook) })
            Assert.Single(typeof(PaymentsController).GetMethod(name)!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        foreach (var name in new[] { nameof(PaymentsController.GenerateQr), nameof(PaymentsController.GetPaymentStatus) })
            Assert.Empty(typeof(PaymentsController).GetMethod(name)!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    [Fact]
    public async Task PublicVietQrEndpoint_PreservesExactRawBodyAndSignatureHeader()
    {
        var sender = Substitute.For<ISender>();
        const string body = "{ \"gateway\":\"Bank\", \"transactionDate\":\"2026-10-22\",\"accountNumber\":\"123\", \"amount\":180000, \"content\":\"POS0123456789ABCDEF0123\", \"transferType\":\"in\", \"transactionId\":789 }";
        var bytes = Encoding.UTF8.GetBytes(body);
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes("test-secret"), bytes));
        ProcessVietQrWebhookCommand? received = null;
        sender.Send(Arg.Any<ProcessVietQrWebhookCommand>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            received = call.Arg<ProcessVietQrWebhookCommand>();
            return received.Signature == signature && received.RawBody.SequenceEqual(bytes)
                ? Result<WebhookAckDto>.Success(new()) : Result<WebhookAckDto>.Failure(PaymentErrors.InvalidWebhook);
        });
        await using var app = Build(sender);
        await app.StartAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook/vietqr")
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("X-VietQR-Signature", signature);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<VietQrWebhookResponse>())!.Success);
        Assert.NotNull(received);
        Assert.Equal(789, received.Payload.TransactionId);
        var unsigned = await client.PostAsync("/api/v1/payments/webhook/vietqr", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, unsigned.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/payments/qr/generate",
            new GenerateQrPaymentRequest(Guid.NewGuid(), "MoMo"))).StatusCode);
    }

    [Theory]
    [InlineData("momo")]
    [InlineData("vietqr")]
    public async Task MalformedWebhook_ReturnsGenericProviderResponse(string provider)
    {
        await using var app = Build(Substitute.For<ISender>());
        await app.StartAsync();
        var response = await app.GetTestClient().PostAsync("/api/v1/payments/webhook/" + provider,
            new StringContent("{bad json", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("JsonException", await response.Content.ReadAsStringAsync());
        Assert.Contains("message", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PaymentBodiesAreNeverAddedToDiagnosticLogs()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProcessMomoWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<WebhookAckDto>.Success(new()));
        var diagnostic = Substitute.For<Serilog.IDiagnosticContext>();
        await using var app = Build(sender, diagnostic);
        await app.StartAsync();
        var response = await app.GetTestClient().PostAsJsonAsync("/api/v1/payments/webhook/momo",
            new MomoWebhookRequest("test", "ref", "ref", 1000, "Order", "momo_wallet", 123,
                0, "Success", "qr", 1700000000000, "", "signature-must-never-be-logged"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(diagnostic.ReceivedCalls());
    }

    private static WebApplication Build(ISender sender, Serilog.IDiagnosticContext? diagnostic = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(sender);
        builder.Services.AddSingleton(diagnostic ?? Substitute.For<Serilog.IDiagnosticContext>());
        builder.Services.AddControllers().AddApplicationPart(typeof(PaymentsController).Assembly);
        // Unauthenticated generate/status requests get 401 without requiring a JWT setup in this transport test.
        builder.Services.AddAuthentication("Test").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
            AnonymousHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseRequestResponseLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    private sealed class AnonymousHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
    }
}
