using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using POS.Api.Controllers;
using POS.Api.Notifications;
using POS.Api.Options;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Notifications;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments.DTOs;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Infrastructure.Payments;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace POS.Api.Tests.Payments;

public class PaymentHubIntegrationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(PaymentMethod.MoMo, true, HttpTransportType.LongPolling)]
    [InlineData(PaymentMethod.MoMo, false, HttpTransportType.WebSockets)]
    [InlineData(PaymentMethod.VietQR, true, HttpTransportType.WebSockets)]
    public async Task SignedWebhook_CommitsThenDeliversContractToRealClient(PaymentMethod method, bool success, HttpTransportType transport)
    {
        await using var f = new PaymentApplicationFactory(method);
        using var http = f.Http();
        await using var connection = f.Connection(f.Cashier, transport: transport);
        var received = new TaskCompletionSource<PaymentStatusResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<PaymentStatusResponse>(success ? PaymentHubEvents.PaymentSuccess : PaymentHubEvents.PaymentFailed,
            payment => { Assert.True(f.Uow.Committed); received.TrySetResult(payment); });
        await connection.StartAsync();
        var subscribed = await connection.InvokeAsync<ApiResponse<PaymentStatusResponse>>("SubscribePayment", f.Payment.Id);
        Assert.True(subscribed.Success);
        Assert.Equal("Pending", subscribed.Data!.Status);

        var stopwatch = Stopwatch.StartNew();
        using var response = await f.Webhook(http, success);
        Assert.Equal(method == PaymentMethod.MoMo ? HttpStatusCode.NoContent : HttpStatusCode.OK, response.StatusCode);
        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(f.Payment.Id, result.PaymentId);
        Assert.Equal(f.Order.Id, result.OrderId);
        Assert.Equal(method.ToString(), result.Method);
        Assert.Equal(success ? "Success" : "Failed", result.Status);
        Assert.Equal(f.Payment.Amount, result.Amount);
        Assert.Equal("123456", result.TransactionRef);
        Assert.Equal(success, result.PaidAt.HasValue);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
        var timing = Assert.Single(f.Logs.Events, e => e.Properties.ContainsKey("WebhookToSignalRMilliseconds") && e.Level == LogEventLevel.Information);
        var milliseconds = Assert.IsType<double>(((ScalarValue)timing.Properties["WebhookToSignalRMilliseconds"]).Value);
        Assert.True(milliseconds >= 0);
        output.WriteLine($"{method}/{transport}/{result.Status}: server webhook -> SignalR = {milliseconds:F3} ms; client = {stopwatch.Elapsed.TotalMilliseconds:F3} ms");
        using var scope = f.Services.CreateScope();
        Assert.IsType<SignalRRealtimeEventPublisher>(Assert.Single(scope.ServiceProvider.GetServices<IRealtimeEventPublisher>()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WebhookBeforeSubscribe_ImmediatelyReplaysTerminalSnapshot(bool success)
    {
        await using var f = new PaymentApplicationFactory();
        using var http = f.Http();
        using var webhook = await f.Webhook(http, success);
        Assert.Equal(HttpStatusCode.NoContent, webhook.StatusCode);
        await using var connection = f.Connection(f.Cashier);
        var received = new TaskCompletionSource<PaymentStatusResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<PaymentStatusResponse>(success ? PaymentHubEvents.PaymentSuccess : PaymentHubEvents.PaymentFailed,
            payment => received.TrySetResult(payment));
        await connection.StartAsync();
        Assert.True((await connection.InvokeAsync<ApiResponse<PaymentStatusResponse>>("SubscribePayment", f.Payment.Id)).Success);
        Assert.Equal(success ? "Success" : "Failed", (await received.Task.WaitAsync(TimeSpan.FromSeconds(2))).Status);
    }

    [Fact]
    public async Task AnonymousOrInvalidToken_CannotConnect()
    {
        await using var f = new PaymentApplicationFactory();
        using var http = f.Http();
        foreach (var query in new[] { "", "&access_token=invalid-token" })
        {
            using var response = await http.PostAsync("/hubs/payment/negotiate?negotiateVersion=1" + query, null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnotherStoreOrShift_CannotSubscribeOrReceivePayment(bool otherStore)
    {
        await using var f = new PaymentApplicationFactory();
        using var http = f.Http();
        await using var connection = f.Connection(otherStore ? f.OtherStore : f.OtherShift);
        var notifications = new ConcurrentQueue<PaymentStatusResponse>();
        connection.On<PaymentStatusResponse>(PaymentHubEvents.PaymentSuccess, notifications.Enqueue);
        await connection.StartAsync();
        var denied = await connection.InvokeAsync<ApiResponse<PaymentStatusResponse>>("SubscribePayment", f.Payment.Id);
        Assert.False(denied.Success);
        Assert.Null(denied.Data);
        Assert.Equal(otherStore ? "PAYMENT.INVALID_STORE" : "PAYMENT.INVALID_SHIFT", denied.Error!.Code);
        using var response = await f.Webhook(http, true);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // A subsequent invocation is an ordering barrier on the same connection, without a sleep.
        await connection.InvokeAsync("UnsubscribePayment", f.Payment.Id);
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task MissingPermission_IsRejectedByRealMediatRPipeline()
    {
        await using var f = new PaymentApplicationFactory();
        await using var connection = f.Connection(f.NoPermission);
        await connection.StartAsync();
        var denied = await connection.InvokeAsync<ApiResponse<PaymentStatusResponse>>("SubscribePayment", f.Payment.Id);
        Assert.Equal("Authorization.Forbidden", denied.Error!.Code);
    }

    [Theory]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.WebSockets)]
    public async Task QueryToken_AuthenticatesHubOnly_AndNeverAppearsInSerilogRequestEvents(HttpTransportType transport)
    {
        await using var f = new PaymentApplicationFactory();
        using var http = f.Http();
        var token = f.Token(f.Cashier);
        await using (var connection = f.Connection(f.Cashier, queryToken: true, transport: transport, tokenOverride: token))
        {
            await connection.StartAsync();
            Assert.True((await connection.InvokeAsync<ApiResponse<PaymentStatusResponse>>("SubscribePayment", f.Payment.Id)).Success);
            await connection.StopAsync();
        }
        using var rest = await http.GetAsync($"/api/v1/payments/{f.Payment.Id}/status?access_token={token}");
        Assert.Equal(HttpStatusCode.Unauthorized, rest.StatusCode);
        var requestEvents = f.Logs.Events.Where(e => e.MessageTemplate.Text.StartsWith("HTTP {RequestMethod}", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(requestEvents);
        Assert.Contains(requestEvents, e => e.Properties["RequestPath"].ToString().Contains("/hubs/payment"));
        foreach (var entry in requestEvents)
        {
            Assert.DoesNotContain("access_token", entry.RenderMessage(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(token, entry.RenderMessage());
            Assert.DoesNotContain("access_token", string.Join(" ", entry.Properties.Select(pair => pair.Value.ToString())), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(token, string.Join(" ", entry.Properties.Select(pair => pair.Value.ToString())));
            Assert.Null(entry.Exception);
            Assert.False(entry.Properties.ContainsKey("RequestBody"));
            Assert.False(entry.Properties.ContainsKey("ResponseBody"));
        }
    }

    [Fact]
    public async Task EmptyOriginList_RejectsCrossOriginHttpAndWebSocket()
    {
        await using var f = new PaymentApplicationFactory();
        using var http = f.Http();
        Assert.Empty(f.Services.GetRequiredService<IOptions<PaymentHubOptions>>().Value.AllowedOrigins);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/hubs/payment/negotiate?negotiateVersion=1&access_token=" + f.Token(f.Cashier));
        request.Headers.Add("Origin", "https://untrusted.example");
        using var response = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        var websocket = f.Server.CreateWebSocketClient();
        websocket.ConfigureRequest = request => request.Headers.Origin = "https://untrusted.example";
        await Assert.ThrowsAsync<InvalidOperationException>(() => websocket.ConnectAsync(
            new Uri("wss://localhost/hubs/payment?access_token=" + f.Token(f.Cashier)), default));
    }

    [Fact]
    public async Task ExplicitOrigin_ReceivesCorsHeadersWithCredentials()
    {
        await using var f = new PaymentApplicationFactory(allowedOrigins: ["https://cashier.example"]);
        using var http = f.Http();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/hubs/payment/negotiate");
        request.Headers.Add("Origin", "https://cashier.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        using var response = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://cashier.example", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    private sealed class PaymentApplicationFactory : WebApplicationFactory<PaymentsController>
    {
        private readonly string signingKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        private readonly string gatewaySecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        private readonly string[] allowedOrigins;
        private readonly Serilog.Core.Logger logger;
        private readonly ILoggerFactory publisherLogs;
        public CaptureSink Logs { get; } = new();
        public TestUnitOfWork Uow { get; } = new();
        public Employee Cashier { get; } = new("Cashier", "cashier", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());
        public Employee OtherStore { get; } = new("Other", "other", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());
        public Employee OtherShift { get; }
        public Employee NoPermission { get; }
        public Order Order { get; }
        public Shift Shift { get; }
        public Payment Payment { get; }

        public PaymentApplicationFactory(PaymentMethod method = PaymentMethod.MoMo, string[]? allowedOrigins = null)
        {
            this.allowedOrigins = allowedOrigins ?? [];
            OtherShift = new("Other shift", "other-shift", "hash", "hash", Guid.NewGuid(), storeId: Cashier.StoreId);
            NoPermission = new("No permission", "no-permission", "hash", "hash", Guid.NewGuid(), storeId: Cashier.StoreId);
            Shift = Shift.Open(Cashier.StoreId!.Value, Cashier.Id, 0);
            Order = Order.CreateDraft(Cashier.StoreId.Value, Shift.Id, Cashier.Id);
            Set(Order, nameof(Order.Status), OrderStatus.Confirmed);
            Set(Order, nameof(Order.GrandTotal), 1000m);
            Payment = Payment.CreatePendingQr(Order.Id, method, 100, "POS0123456789ABCDEF0123",
                new QrSessionDto("qr", null, DateTimeOffset.UtcNow.AddMinutes(15)).ToJson());
            Set(Payment, nameof(Payment.Order), Order);
            Order.Payments.Add(Payment);
            logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Sink(Logs).CreateLogger();
            publisherLogs = new Serilog.Extensions.Logging.SerilogLoggerFactory(logger, dispose: false);
        }

        public HttpClient Http() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        public string Token(Employee employee) => Services.GetRequiredService<ITokenService>().CreateAccessToken(
            new(employee.Id, "Employee", employee.RoleId, employee.StoreId, false, ["payments:read"])).AccessToken;

        public HubConnection Connection(Employee employee, bool queryToken = false, HttpTransportType transport = HttpTransportType.LongPolling,
            string? tokenOverride = null)
        {
            var token = tokenOverride ?? Token(employee);
            return new HubConnectionBuilder().WithUrl("https://localhost/hubs/payment" + (queryToken ? "?access_token=" + token : ""), options =>
            {
                options.Transports = transport;
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                if (!queryToken) options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.WebSocketFactory = async (context, ct) =>
                {
                    var websocket = Server.CreateWebSocketClient();
                    if (!queryToken) websocket.ConfigureRequest = request => request.Headers.Authorization = "Bearer " + token;
                    return await websocket.ConnectAsync(context.Uri, ct);
                };
            }).Build();
        }

        public Task<HttpResponseMessage> Webhook(HttpClient client, bool success)
        {
            if (Payment.Method == PaymentMethod.MoMo)
            {
                var extra = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new { orderId = Order.Id }));
                var dto = new MomoWebhookDto("test-partner", Payment.MerchantReference!, Payment.MerchantReference!,
                    100, "Order", "momo_wallet", 123456, success ? 0 : 1006, success ? "Success" : "Failed", "qr",
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), extra, "");
                dto = dto with { Signature = PaymentHmac.Sign(gatewaySecret, MomoWebhookSignatureVerifier.SigningData("test-access", dto)) };
                return client.PostAsJsonAsync("/api/v1/payments/webhook/momo", dto);
            }
            var vietqr = new VietQrWebhookRequest("Bank", "2026-10-22", "123", null, 100,
                Payment.MerchantReference!, "in", null, 123456, null);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(vietqr, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook/vietqr")
            { Content = new ByteArrayContent(bytes) };
            request.Content.Headers.ContentType = new("application/json");
            request.Headers.Add("X-VietQR-Signature", Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(gatewaySecret), bytes)));
            return SendAndDispose(client, request);
        }

        private static async Task<HttpResponseMessage> SendAndDispose(HttpClient client, HttpRequestMessage request)
        {
            using (request) return await client.SendAsync(request);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var config = new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = signingKey, ["Jwt:Issuer"] = "pos-payment-tests", ["Jwt:Audience"] = "pos-payment-client",
                ["Jwt:AccessTokenExpiryHours"] = "1", ["Auth:PinLookupSecret"] = signingKey,
                ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                ["Redis:ConnectionString"] = "localhost:6379", ["Serilog:LokiUrl"] = "",
                ["Payments:MoMo:PartnerCode"] = "test-partner", ["Payments:MoMo:AccessKey"] = "test-access",
                ["Payments:MoMo:SecretKey"] = gatewaySecret,
                ["Payments:VietQR:AccountNo"] = "123", ["Payments:VietQR:WebhookSecret"] = gatewaySecret
            };
            for (var index = 0; index < allowedOrigins.Length; index++) config[$"PaymentHub:AllowedOrigins:{index}"] = allowedOrigins[index];
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(config));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var employees = Substitute.For<IEmployeeRepository>();
                foreach (var person in new[] { Cashier, OtherStore, OtherShift, NoPermission })
                    employees.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
                var payments = Substitute.For<IPaymentRepository>();
                payments.GetByIdWithOrderAsync(Payment.Id, Arg.Any<CancellationToken>()).Returns(_ => Payment);
                payments.GetForWebhookAsync(Payment.Method, Payment.MerchantReference!, Arg.Any<CancellationToken>()).Returns(Payment);
                var shifts = Substitute.For<IShiftRepository>();
                shifts.GetByIdAsync(Shift.Id, Arg.Any<CancellationToken>()).Returns(Shift);
                var cache = Substitute.For<ICacheService>();
                cache.GetAsync<string[]>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
                    call.Arg<string>() == $"perm:{NoPermission.Id}" ? [] : new[] { "payments:read" });
                services.RemoveAll<IEmployeeRepository>(); services.AddSingleton(employees);
                services.RemoveAll<IPaymentRepository>(); services.AddSingleton(payments);
                services.RemoveAll<IShiftRepository>(); services.AddSingleton(shifts);
                services.RemoveAll<ICacheService>(); services.AddSingleton(cache);
                services.RemoveAll<IAuditLogRepository>(); services.AddSingleton(Substitute.For<IAuditLogRepository>());
                services.RemoveAll<IUnitOfWork>(); services.AddSingleton<IUnitOfWork>(Uow);
                services.AddSingleton(publisherLogs.CreateLogger<SignalRRealtimeEventPublisher>());
                services.Configure<RequestLoggingOptions>(options => options.Logger = logger);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            publisherLogs.Dispose();
            await logger.DisposeAsync();
        }

        private static void Set(object target, string property, object value) => target.GetType().GetProperty(property)!.SetValue(target, value);
    }

    private sealed class CaptureSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
    }

    // Real JWT, MediatR/validation/authorization, gateway HMAC verification, processor and SignalR.
    // Persistence is substituted; PostgreSQL transaction semantics remain covered by T46 tests.
    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public bool Committed { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public async Task<Result<T>> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<Result<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            Committed = false;
            var result = await operation(cancellationToken);
            Committed = result.IsSuccess;
            return result;
        }
    }
}
