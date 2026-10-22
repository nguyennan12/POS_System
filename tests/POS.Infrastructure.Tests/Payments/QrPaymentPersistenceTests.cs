using System.Data.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Notifications;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Orders.Events;
using POS.Application.UseCases.Payments;
using POS.Application.UseCases.Payments.Commands.GenerateQrPayment;
using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;
using POS.Domain.Rbac;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Tests.Stores;

namespace POS.Infrastructure.Tests.Payments;

public class QrPaymentPersistenceTests
{
    [PostgresFact]
    public async Task ConcurrentDuplicateWebhooks_CommitOnePaymentInvoiceAuditAndRealtime()
    {
        await using var f = await Database.CreateAsync();
        var pause = new ReadBarrier("FROM payments");
        await using var a = f.Db(pause);
        await using var b = f.Db(pause);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var first = f.Processor(a, f.Message());
        var second = f.Processor(b, f.Message());
        var results = await Task.WhenAll(first.ProcessAsync(PaymentMethod.MoMo, new(), timeout.Token),
            second.ProcessAsync(PaymentMethod.MoMo, new(), timeout.Token));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Code));
        await using var verify = f.Db();
        Assert.Equal(PaymentStatus.Success, (await verify.Payments.SingleAsync()).Status);
        Assert.Equal(OrderStatus.Paid, (await verify.Orders.SingleAsync(o => o.Id == f.OrderId)).Status);
        Assert.Equal(1, await verify.Invoices.CountAsync());
        Assert.Equal(1, await verify.AuditLogs.CountAsync());
        Assert.Equal(1, f.Realtime.Count);
        Assert.True(f.Realtime.AllAfterCommit);
        Assert.Equal(2, pause.Arrivals);
    }

    [PostgresFact]
    public async Task ConcurrentDifferentPayments_AggregateOnlySuccessfulAmounts()
    {
        await using var f = await Database.CreateAsync(40);
        var other = await f.AddPaymentAsync(f.OrderId, PaymentMethod.VietQR, 60);
        var pause = new ReadBarrier("FROM payments");
        await using var a = f.Db(pause);
        await using var b = f.Db(pause);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var results = await Task.WhenAll(
            f.Processor(a, f.Message()).ProcessAsync(PaymentMethod.MoMo, new(), timeout.Token),
            f.Processor(b, f.Message(other, "second-provider-ref")).ProcessAsync(PaymentMethod.VietQR, new(), timeout.Token));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Code));
        await using var verify = f.Db();
        Assert.Equal(2, await verify.Payments.CountAsync(p => p.Status == PaymentStatus.Success));
        Assert.Equal(OrderStatus.Paid, (await verify.Orders.SingleAsync(o => o.Id == f.OrderId)).Status);
        Assert.Equal(1, await verify.Invoices.CountAsync());
        Assert.Equal(2, await verify.AuditLogs.CountAsync());
        Assert.Equal(2, f.Realtime.Count);
    }

    [PostgresFact]
    public async Task ConcurrentProviderReferenceAcrossOrders_RejectsSecondSettlement()
    {
        await using var f = await Database.CreateAsync();
        var other = await f.AddPaymentAsync(f.SecondOrderId, PaymentMethod.MoMo, 100);
        var pause = new ReadBarrier("FROM payments");
        await using var a = f.Db(pause);
        await using var b = f.Db(pause);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var results = await Task.WhenAll(
            f.Processor(a, f.Message()).ProcessAsync(PaymentMethod.MoMo, new(), timeout.Token),
            f.Processor(b, f.Message(other)).ProcessAsync(PaymentMethod.MoMo, new(), timeout.Token));
        Assert.Single(results, r => r.IsSuccess);
        Assert.Single(results, r => r.IsFailure);
        await using var verify = f.Db();
        Assert.Equal(1, await verify.Payments.CountAsync(p => p.Status == PaymentStatus.Success));
        Assert.Equal(1, await verify.Payments.CountAsync(p => p.Status == PaymentStatus.Pending));
        Assert.Equal(1, await verify.Invoices.CountAsync());
        Assert.Equal(1, await verify.AuditLogs.CountAsync());
    }

    [PostgresFact]
    public async Task ConcurrentGenerate_ReusesOneReservation()
    {
        await using var f = await Database.CreateAsync(seedPayment: false);
        var pause = new ReadBarrier("FROM orders");
        await using var a = f.Db(pause);
        await using var b = f.Db(pause);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var results = await Task.WhenAll(f.Generator(a).Handle(new(f.OrderId, "MoMo"), timeout.Token),
            f.Generator(b).Handle(new(f.OrderId, "MoMo"), timeout.Token));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Code));
        Assert.Equal(results[0].Value!.PaymentId, results[1].Value!.PaymentId);
        await using var verify = f.Db();
        Assert.Equal(1, await verify.Payments.CountAsync());
        Assert.Equal(1, await verify.AuditLogs.CountAsync());
        Assert.Null((await verify.Payments.SingleAsync()).TransactionRef);
    }

    [PostgresFact]
    public async Task ExpiredQr_GenerateReleasesPendingUniqueIndex_InSameTransaction()
    {
        await using var f = await Database.CreateAsync();
        await using (var seed = f.Db())
        {
            var payment = await seed.Payments.SingleAsync();
            payment.SetQrMetadata(new QrSessionDto("old-qr", null, DateTimeOffset.UtcNow.AddMinutes(-1)).ToJson());
            await seed.SaveChangesAsync();
        }
        await using var db = f.Db();
        var result = await f.Generator(db).Handle(new(f.OrderId, "MoMo"), default);
        Assert.True(result.IsSuccess, result.Error.Code);
        await using var verify = f.Db();
        Assert.Equal(1, await verify.Payments.CountAsync(p => p.Status == PaymentStatus.Timeout));
        Assert.Equal(1, await verify.Payments.CountAsync(p => p.Status == PaymentStatus.Pending));
    }

    [PostgresFact]
    public async Task PartialPaymentPersists_AndInvoiceFailureRollsBackSettlement()
    {
        await using var f = await Database.CreateAsync(40);
        await using (var db = f.Db())
            Assert.True((await f.Processor(db, f.Message()).ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        await using (var verify = f.Db())
        {
            Assert.Equal(OrderStatus.Confirmed, (await verify.Orders.SingleAsync(o => o.Id == f.OrderId)).Status);
            Assert.Equal(PaymentStatus.Success, (await verify.Payments.SingleAsync()).Status);
            Assert.Empty(await verify.Invoices.ToListAsync());
        }
        var remaining = await f.AddPaymentAsync(f.OrderId, PaymentMethod.MoMo, 60);
        await using (var db = f.Db())
        {
            var processor = f.Processor(db, f.Message(remaining, "another-ref"), failInvoice: true);
            await Assert.ThrowsAsync<InjectedInvoiceFailure>(() => processor.ProcessAsync(PaymentMethod.MoMo, new(), default));
        }
        await using var after = f.Db();
        Assert.Equal(PaymentStatus.Pending, (await after.Payments.SingleAsync(p => p.Id == remaining.Id)).Status);
        Assert.Equal(OrderStatus.Confirmed, (await after.Orders.SingleAsync(o => o.Id == f.OrderId)).Status);
        Assert.Empty(await after.Invoices.ToListAsync());
        Assert.Equal(1, await after.AuditLogs.CountAsync());
        Assert.Equal(1, f.Realtime.Count);
    }

    private sealed class InjectedInvoiceFailure : Exception;

    // Stop both readers after a serializable snapshot is established, before either can write.
    private sealed class ReadBarrier(string table) : DbCommandInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Arrivals => Math.Min(arrivals, 2);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT", StringComparison.Ordinal) && command.CommandText.Contains(table, StringComparison.Ordinal))
            {
                var n = Interlocked.Increment(ref arrivals);
                if (n == 2) ready.TrySetResult();
                if (n <= 2) await ready.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class Gateway(VerifiedWebhookDto message, PaymentMethod method) : IPaymentGateway
    {
        public PaymentMethod Method => method;
        public Task<Result<QrSessionDto>> GenerateAsync(Payment payment, QrSessionDto session, CancellationToken ct) =>
            Task.FromResult(Result<QrSessionDto>.Success(session with { QrCodeData = "qr:" + payment.MerchantReference }));
        public Result<VerifiedWebhookDto> VerifyWebhook(WebhookEnvelopeDto webhook) => message;
    }

    private sealed class RealtimeSpy : IRealtimeEventPublisher
    {
        private int count;
        public int Count => count;
        public bool AllAfterCommit { get; private set; } = true;
        public Func<bool> Committed { get; set; } = () => true;
        public Task PublishPaymentAsync(PaymentRealtimeDto notification, CancellationToken cancellationToken)
        {
            AllAfterCommit &= Committed();
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        }
    }

    private sealed class Database : IAsyncDisposable
    {
        private readonly string schema = "qr_payment_test_" + Guid.NewGuid().ToString("N");
        private string connection = null!;
        public Guid OrderId { get; private set; }
        public Guid SecondOrderId { get; private set; }
        private Guid storeId;
        private Guid employeeId;
        private Payment payment = null!;
        public RealtimeSpy Realtime { get; } = new();

        public static async Task<Database> CreateAsync(decimal amount = 100, bool seedPayment = true)
        {
            var f = new Database();
            f.connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POS_TEST_POSTGRES"))
            { SearchPath = f.schema, Pooling = false }.ConnectionString;
            await using var admin = new NpgsqlConnection(f.connection);
            await admin.OpenAsync();
            await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{f.schema}\"", admin)) await create.ExecuteNonQueryAsync();
            try
            {
                await using var db = f.Db();
                await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
                var store = new Store("QR store");
                var role = new Role("Cashier", true);
                var employee = new Employee("Cashier", "qr_cashier", "unused", "unused", role.Id, storeId: store.Id);
                employee.setPinLookUpHash(new string('a', 64));
                var register = new PosRegister(store.Id, "QR register", "QR-01");
                var shift = Shift.Open(store.Id, register.Id, employee.Id, 0);
                var first = Order.CreateDraft(store.Id, shift.Id, employee.Id);
                var second = Order.CreateDraft(store.Id, shift.Id, employee.Id);
                foreach (var order in new[] { first, second })
                {
                    typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Confirmed);
                    typeof(Order).GetProperty(nameof(Order.GrandTotal))!.SetValue(order, 100m);
                    typeof(Order).GetProperty(nameof(Order.Subtotal))!.SetValue(order, 100m);
                }
                db.AddRange(store, role, employee, register, shift, first, second);
                await db.SaveChangesAsync();
                f.OrderId = first.Id;
                f.SecondOrderId = second.Id;
                f.storeId = store.Id;
                f.employeeId = employee.Id;
                if (seedPayment) f.payment = await f.AddPaymentAsync(first.Id, PaymentMethod.MoMo, amount);
                return f;
            }
            catch { await f.DisposeAsync(); throw; }
        }

        public AppDbContext Db(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection).AddInterceptors(interceptors).Options);

        public async Task<Payment> AddPaymentAsync(Guid orderId, PaymentMethod method, decimal amount)
        {
            var p = Payment.CreatePendingQr(orderId, method, amount, "POS" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(),
                new QrSessionDto("qr", null, DateTimeOffset.UtcNow.AddMinutes(15)).ToJson());
            await using var db = Db();
            db.Payments.Add(p);
            await db.SaveChangesAsync();
            return p;
        }

        public VerifiedWebhookDto Message(Payment? target = null, string transactionRef = "same-provider-ref")
        {
            target ??= payment;
            return new(target.MerchantReference!, transactionRef, target.Amount, true, DateTimeOffset.UtcNow, target.OrderId);
        }

        public QrWebhookProcessor Processor(AppDbContext db, VerifiedWebhookDto message, bool failInvoice = false)
        {
            var publisher = Substitute.For<IPublisher>();
            var invoices = new OrderPaidInvoiceHandler(new OrderRepository(db), new StoreRepository(db),
                new InvoiceRepository(db), NullLogger<OrderPaidInvoiceHandler>.Instance);
            publisher.Publish(Arg.Any<OrderPaidDomainEvent>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                await invoices.Handle(call.Arg<OrderPaidDomainEvent>(), call.Arg<CancellationToken>());
                if (failInvoice)
                {
                    await db.SaveChangesAsync(); // Ensure rollback is tested after actual SQL writes.
                    throw new InjectedInvoiceFailure();
                }
            });
            var method = db.Payments.AsNoTracking().Single(p => p.MerchantReference == message.MerchantReference).Method;
            // Each caller has its own DbContext; count only after ExecuteSerializableAsync disposed its transaction.
            var realtime = new ContextRealtime(Realtime, db);
            return new([new Gateway(message, method)], new PaymentRepository(db), new AuditLogRepository(db),
                new UnitOfWork(db), publisher, realtime, TimeProvider.System, NullLogger<QrWebhookProcessor>.Instance);
        }

        public GenerateQrPaymentCommandHandler Generator(AppDbContext db)
        {
            var access = Substitute.For<IStoreAccessPolicy>();
            access.CanAccessStore(storeId).Returns(true);
            var user = Substitute.For<ICurrentUser>();
            user.EmployeeId.Returns(employeeId);
            return new(new OrderRepository(db), new PaymentRepository(db), new ShiftRepository(db), access,
                new UnitOfWork(db), [new Gateway(new("", "", 0, false, default), PaymentMethod.MoMo)], new(), TimeProvider.System,
                new AuditLogRepository(db), user);
        }

        public async ValueTask DisposeAsync()
        {
            await using var admin = new NpgsqlConnection(connection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class ContextRealtime(RealtimeSpy spy, AppDbContext db) : IRealtimeEventPublisher
    {
        public Task PublishPaymentAsync(PaymentRealtimeDto notification, CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            return spy.PublishPaymentAsync(notification, cancellationToken);
        }
    }
}
