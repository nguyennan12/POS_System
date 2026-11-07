using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using POS.Api.Hubs;
using POS.Api.Notifications;
using POS.Application.UseCases.Payments.DTOs;
using POS.Contracts.V1.Payments;

namespace POS.Api.Tests.Payments;

public class SignalRRealtimeEventPublisherTests
{
    [Theory]
    [InlineData("Success", PaymentHubEvents.PaymentSuccess)]
    [InlineData("Failed", PaymentHubEvents.PaymentFailed)]
    public async Task PublishesExactContractToPaymentGroup(string status, string eventName)
    {
        var f = new Fixture(status);
        using var cts = new CancellationTokenSource();
        await f.Publisher.PublishPaymentAsync(f.Notification, cts.Token);
        f.Clients.Received(1).Group($"payment:{f.Notification.Payment.PaymentId:D}");
        var dto = f.Notification.Payment;
        await f.Proxy.Received(1).SendCoreAsync(eventName, Arg.Is<object?[]>(args => args.Length == 1 &&
            Equals(args[0], new PaymentStatusResponse(dto.PaymentId, dto.OrderId, dto.Method, dto.Amount,
                dto.Status, dto.TransactionRef, dto.PaidAt))), cts.Token);
        Assert.DoesNotContain(f.Clients.ReceivedCalls(), call => call.GetMethodInfo().Name is "get_All" or "User");
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Timeout")]
    public async Task DoesNotPublishUnsupportedStatuses(string status)
    {
        var f = new Fixture(status);
        await f.Publisher.PublishPaymentAsync(f.Notification, default);
        Assert.Empty(f.Clients.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportFailureOrCancellation_IsContainedWithoutLoggingExceptionDetails(bool canceled)
    {
        var f = new Fixture("Success");
        Exception failure = canceled ? new OperationCanceledException("access_token=secret")
            : new InvalidOperationException("access_token=secret");
        f.Proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(failure));
        await f.Publisher.PublishPaymentAsync(f.Notification, default);
        var log = Assert.Single(f.Logger.ReceivedCalls());
        Assert.Null(log.GetArguments()[3]);
        Assert.DoesNotContain("secret", log.GetArguments()[2]!.ToString());
    }

    private sealed class Fixture
    {
        public IHubClients Clients { get; } = Substitute.For<IHubClients>();
        public IClientProxy Proxy { get; } = Substitute.For<IClientProxy>();
        public ILogger<SignalRRealtimeEventPublisher> Logger { get; } = Substitute.For<ILogger<SignalRRealtimeEventPublisher>>();
        public PaymentRealtimeDto Notification { get; }
        public SignalRRealtimeEventPublisher Publisher { get; }

        public Fixture(string status)
        {
            var hub = Substitute.For<IHubContext<PaymentHub>>();
            hub.Clients.Returns(Clients);
            Clients.Group(Arg.Any<string>()).Returns(Proxy);
            Notification = new(Guid.NewGuid(), new(Guid.NewGuid(), Guid.NewGuid(), "VietQR", 180000,
                status, "provider-ref", DateTimeOffset.UtcNow));
            Publisher = new(hub, new HttpContextAccessor(), Logger);
        }
    }
}
