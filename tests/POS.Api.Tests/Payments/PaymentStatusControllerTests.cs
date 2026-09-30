using MediatR;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application.UseCases.Payments.Errors;
using POS.Application.UseCases.Payments.Queries.GetPaymentStatus;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;

namespace POS.Api.Tests.Payments;

public class PaymentStatusControllerTests
{
    [Fact]
    public async Task GetPaymentStatus_ReturnsDocumentedEnvelope()
    {
        var id = Guid.NewGuid();
        var response = new PaymentStatusResponse(id, Guid.NewGuid(), "Card", 40, "Success", "ref", null);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Is<GetPaymentStatusQuery>(query => query.Id == id), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentStatusResponse>.Success(response));

        var result = await new PaymentsController(sender).GetPaymentStatus(id, default);

        Assert.Same(response, Assert.IsType<ApiResponse<PaymentStatusResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data);
    }

    [Theory]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Validation, 400)]
    public async Task GetPaymentStatus_MapsFailureStatus(ErrorType errorType, int expectedStatus)
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetPaymentStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentStatusResponse>.Failure(new Error(errorType, "TEST.ERROR", "test")));

        var result = await new PaymentsController(sender).GetPaymentStatus(Guid.NewGuid(), default);

        Assert.Equal(expectedStatus, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public void PaymentRoute_IsDocumentedRoute_AndOldOrderRouteIsAbsent()
    {
        Assert.Equal("api/v1/payments", typeof(PaymentsController)
            .GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template);
        Assert.Equal("{id:guid}/status", typeof(PaymentsController)
            .GetMethod(nameof(PaymentsController.GetPaymentStatus))!
            .GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template);
        Assert.DoesNotContain(typeof(OrdersController).GetMethods(), method =>
            method.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>()
                .Any(attribute => attribute.Template?.Contains("payments") == true));
    }

    [Fact]
    public async Task HttpRouting_UsesPaymentRoute_AndOldOrderRouteReturns404()
    {
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetPaymentStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentStatusResponse>.Success(new(
                paymentId, orderId, "Card", 40, "Success", null, null)));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(sender);
        builder.Services.AddControllers().AddApplicationPart(typeof(PaymentsController).Assembly);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"/api/v1/payments/{paymentId}/status")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Auth", "yes");
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"/api/v1/payments/{paymentId}/status")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/orders/{orderId}/payments/{paymentId}/status")).StatusCode);
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Request.Headers["X-Test-Auth"] != "yes"
                ? Task.FromResult(AuthenticateResult.NoResult())
                : Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test")], "Test")),
                "Test")));
    }
}
