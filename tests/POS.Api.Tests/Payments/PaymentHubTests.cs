using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using NSubstitute.Extensions;
using POS.Api.Auth;
using POS.Api.Hubs;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Application.UseCases.Payments.Queries.AuthorizePaymentSubscription;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;

namespace POS.Api.Tests.Payments;

public class PaymentHubTests
{
    [Theory]
    [InlineData("Success", PaymentHubEvents.PaymentSuccess)]
    [InlineData("Failed", PaymentHubEvents.PaymentFailed)]
    public async Task TerminalSnapshot_IsSentImmediatelyAndOnlyToCaller(string status, string eventName)
    {
        var f = new Fixture(status);
        var result = await f.Hub.SubscribePayment(f.Payment.PaymentId);
        Assert.True(result.Success);
        Assert.Equal(status, result.Data!.Status);
        await f.Caller.Received(1).SendCoreAsync(eventName,
            Arg.Is<object?[]>(args => args.Length == 1 && ((PaymentStatusResponse)args[0]!).PaymentId == f.Payment.PaymentId),
            Arg.Any<CancellationToken>());
        Assert.Equal(new[] { "read", "join", "read" }, f.Trace);
        Assert.Same(f.OriginalContext, f.Accessor.HttpContext);
    }

    [Fact]
    public async Task WebhookBetweenAuthorizationAndJoin_IsRecoveredByFreshSnapshot()
    {
        var f = new Fixture();
        f.Groups.Configure().AddToGroupAsync("connection", $"payment:{f.Payment.PaymentId:D}", Arg.Any<CancellationToken>())
            .Returns(_ => { f.Trace.Add("join"); f.Payment = f.Payment with { Status = "Success" }; return Task.CompletedTask; });
        Assert.Equal("Success", (await f.Hub.SubscribePayment(f.Payment.PaymentId)).Data!.Status);
        await f.Caller.Received(1).SendCoreAsync(PaymentHubEvents.PaymentSuccess, Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnauthorizedSubscription_NeverJoinsGroup()
    {
        var f = new Fixture();
        f.Sender.Configure().Send(Arg.Any<AuthorizePaymentSubscriptionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentStatusDto>.Failure(PaymentErrors.InvalidStore));
        var result = await f.Hub.SubscribePayment(f.Payment.PaymentId);
        Assert.False(result.Success);
        Assert.Equal(PaymentErrors.InvalidStore.Code, result.Error!.Code);
        Assert.Empty(f.Groups.ReceivedCalls());
        Assert.Empty(f.Caller.ReceivedCalls());
    }

    [Fact]
    public async Task AuthorizationLostDuringJoin_RemovesGroupAndReturnsFailure()
    {
        var f = new Fixture();
        f.Sender.Configure().Send(Arg.Any<AuthorizePaymentSubscriptionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentStatusDto>.Success(f.Payment), Result<PaymentStatusDto>.Failure(PaymentErrors.InvalidShift));
        Assert.False((await f.Hub.SubscribePayment(f.Payment.PaymentId)).Success);
        await f.Groups.Received(1).RemoveFromGroupAsync("connection", $"payment:{f.Payment.PaymentId:D}", CancellationToken.None);
        Assert.Empty(f.Caller.ReceivedCalls());
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Timeout")]
    public async Task OtherSnapshots_ReturnWithoutInventingEvents(string status)
    {
        var f = new Fixture(status);
        Assert.Equal(status, (await f.Hub.SubscribePayment(f.Payment.PaymentId)).Data!.Status);
        Assert.Empty(f.Caller.ReceivedCalls());
        await f.Hub.UnsubscribePayment(f.Payment.PaymentId);
        await f.Groups.Received(1).RemoveFromGroupAsync("connection", $"payment:{f.Payment.PaymentId:D}", Arg.Any<CancellationToken>());
    }

    private sealed class Fixture
    {
        public ISender Sender { get; } = Substitute.For<ISender>();
        public IGroupManager Groups { get; } = Substitute.For<IGroupManager>();
        public ISingleClientProxy Caller { get; } = Substitute.For<ISingleClientProxy>();
        public List<string> Trace { get; } = [];
        public HttpContext OriginalContext { get; } = new DefaultHttpContext();
        public IHttpContextAccessor Accessor { get; } = new HttpContextAccessor();
        public PaymentStatusDto Payment { get; set; }
        public PaymentHub Hub { get; }

        public Fixture(string status = "Pending")
        {
            Payment = new(Guid.NewGuid(), Guid.NewGuid(), "MoMo", 40, status, null, null);
            var context = Substitute.For<HubCallerContext>();
            context.ConnectionId.Returns("connection");
            var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([], "Bearer")) };
            var features = new FeatureCollection();
            features.Set<IHttpContextFeature>(new HttpContextFeature { HttpContext = http });
            context.Features.Returns(features);
            var clients = Substitute.For<IHubCallerClients>();
            clients.Caller.Returns(Caller);
            Accessor.HttpContext = OriginalContext;
            var currentUser = new CurrentUser(Accessor);
            Hub = new(Sender, currentUser) { Context = context, Groups = Groups, Clients = clients };
            Sender.Send(Arg.Any<AuthorizePaymentSubscriptionQuery>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                Assert.True(currentUser.IsAuthenticated);
                Assert.Same(OriginalContext, Accessor.HttpContext);
                Trace.Add("read");
                return Result<PaymentStatusDto>.Success(Payment);
            });
            Groups.AddToGroupAsync("connection", $"payment:{Payment.PaymentId:D}", Arg.Any<CancellationToken>())
                .Returns(_ => { Trace.Add("join"); return Task.CompletedTask; });
        }
    }

    private sealed class HttpContextFeature : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; }
    }
}
