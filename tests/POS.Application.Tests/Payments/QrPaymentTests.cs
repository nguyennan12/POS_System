using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Notifications;
using POS.Application.Abstractions.Payments;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Payments;
using POS.Application.UseCases.Payments.Commands.GenerateQrPayment;
using POS.Application.UseCases.Payments.Commands.ExpirePendingPayment;
using POS.Application.UseCases.Payments.Commands.ProcessMomoWebhook;
using POS.Application.UseCases.Payments.Commands.ProcessVietQrWebhook;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Auditing;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;

namespace POS.Application.Tests.Payments;

public class QrPaymentTests
{
    [Theory]
    [InlineData(40, OrderStatus.Confirmed)]
    [InlineData(100, OrderStatus.Paid)]
    public async Task Webhook_PreservesPartialSettlement_AndCompletesExactTotal(decimal amount, OrderStatus expected)
    {
        var f = new Fixture(amount);
        var result = await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Success, f.Payment.Status);
        Assert.Equal(expected, f.Order.Status);
        Assert.True(f.Uow.Committed);
        await f.Events.Received(expected == OrderStatus.Paid ? 1 : 0).Publish(Arg.Any<OrderPaidDomainEvent>(), Arg.Any<CancellationToken>());
        await f.Audit.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.EmployeeId == null && a.Description == "Actor=System"
            && a.StoreId == f.Order.StoreId && a.EntityId == f.Payment.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateWebhook_AcksOnce_AndNeverPublishesBeforeCommit()
    {
        var f = new Fixture(100);
        f.Realtime.PublishPaymentAsync(Arg.Any<PaymentRealtimeDto>(), Arg.Any<CancellationToken>())
            .Returns(_ => { Assert.True(f.Uow.Committed); return Task.CompletedTask; });
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        Assert.Equal(1, f.Uow.SaveCalls);
        await f.Realtime.Received(1).PublishPaymentAsync(Arg.Any<PaymentRealtimeDto>(), Arg.Any<CancellationToken>());
        await f.Events.Received(1).Publish(Arg.Any<OrderPaidDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RealtimeFailure_DoesNotFailCommittedWebhook()
    {
        var f = new Fixture(100);
        f.Realtime.PublishPaymentAsync(Arg.Any<PaymentRealtimeDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("publisher unavailable")));
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        Assert.True(f.Uow.Committed);
        Assert.Equal(OrderStatus.Paid, f.Order.Status);
    }

    [Fact]
    public async Task IntermediateNotification_AcksWithoutSettling()
    {
        var f = new Fixture(100);
        f.Message = f.Message with { Success = false, IsFinal = false };
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        Assert.Equal(PaymentStatus.Pending, f.Payment.Status);
        Assert.Equal(0, f.Uow.SaveCalls);
    }

    [Theory]
    [InlineData("signature")]
    [InlineData("missing")]
    [InlineData("amount")]
    [InlineData("order")]
    [InlineData("duplicate-other-payment")]
    [InlineData("stale")]
    [InlineData("future")]
    [InlineData("timeout")]
    [InlineData("overpayment")]
    public async Task InvalidWebhook_NeverChangesPayment(string failure)
    {
        var f = new Fixture(100);
        switch (failure)
        {
            case "signature": f.VerifyResult = PaymentErrors.InvalidWebhook; break;
            case "missing": f.Payments.GetForWebhookAsync(Arg.Any<PaymentMethod>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Payment?)null); break;
            case "amount": f.Message = f.Message with { Amount = 101 }; break;
            case "order": f.Message = f.Message with { OrderId = Guid.NewGuid() }; break;
            case "duplicate-other-payment": f.Payments.TransactionExistsAsync(Arg.Any<PaymentMethod>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true); break;
            case "stale": f.Message = f.Message with { OccurredAt = DateTimeOffset.UtcNow.AddHours(-1) }; break;
            case "future": f.Message = f.Message with { OccurredAt = DateTimeOffset.UtcNow.AddHours(1) }; break;
            case "timeout": f.Payment.ExpireQr(); break;
            case "overpayment": f.Order.Payments.Add(Payment.CreateSuccess(f.Order.Id, PaymentMethod.Cash, 10)); break;
        }
        var before = f.Payment.Status;
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsFailure);
        Assert.Equal(before, f.Payment.Status);
        Assert.Equal(0, f.Uow.SaveCalls);
        await f.Realtime.DidNotReceive().PublishPaymentAsync(Arg.Any<PaymentRealtimeDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailureIsTerminal_AndOldSuccessfulRetryStillAcks()
    {
        var f = new Fixture(100);
        f.Message = f.Message with { Success = false };
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        Assert.Equal(PaymentStatus.Failed, f.Payment.Status);
        Assert.Equal(OrderStatus.Confirmed, f.Order.Status);
        f.Message = f.Message with { Success = true };
        Assert.True((await f.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsFailure);
        var success = new Fixture(100);
        Assert.True((await success.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
        success.Message = success.Message with { OccurredAt = DateTimeOffset.UtcNow.AddDays(-1) };
        Assert.True((await success.Processor.ProcessAsync(PaymentMethod.MoMo, new(), default)).IsSuccess);
    }

    [Theory]
    [InlineData("state", "PAYMENT.INVALID_ORDER")]
    [InlineData("closed", "PAYMENT.SHIFT_CLOSED")]
    [InlineData("store", "PAYMENT.INVALID_STORE")]
    [InlineData("currency", "PAYMENT.UNSUPPORTED_CURRENCY")]
    public async Task Generate_RejectsInvalidOrderShiftAndStore(string failure, string code)
    {
        var f = new Fixture(100);
        if (failure == "state") Set(f.Order, nameof(Order.Status), OrderStatus.Draft);
        if (failure == "closed") f.Shift.Close(0, 0, null);
        if (failure == "store") f.Access.CanAccessStore(f.Order.StoreId).Returns(false);
        if (failure == "currency") Set(f.Order, nameof(Order.CurrencyCode), "USD");
        var result = await f.Generator.Handle(new(f.Order.Id, "MoMo"), default);
        Assert.Equal(code, result.Error.Code);
        await f.Gateway.DidNotReceive().GenerateAsync(Arg.Any<Payment>(), Arg.Any<QrSessionDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_ReusesPendingQr_AndReservesOnlyDatabaseRemainder()
    {
        var f = new Fixture(40);
        var reused = await f.Generator.Handle(new(f.Order.Id, "MoMo"), default);
        Assert.Equal(f.Payment.Id, reused.Value!.PaymentId);
        await f.Gateway.DidNotReceive().GenerateAsync(Arg.Any<Payment>(), Arg.Any<QrSessionDto>(), Arg.Any<CancellationToken>());
        f.Order.Payments.Clear();
        f.Order.Payments.Add(Payment.CreateSuccess(f.Order.Id, PaymentMethod.Cash, 65, changeAmount: 5));
        Payment? added = null;
        f.Orders.AddPaymentsAsync(Arg.Any<IEnumerable<Payment>>(), Arg.Any<CancellationToken>())
            .Returns(call => { added = call.Arg<IEnumerable<Payment>>().Single(); f.Order.Payments.Add(added); return Task.CompletedTask; });
        f.Payments.GetTrackedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => added);
        f.Gateway.GenerateAsync(Arg.Any<Payment>(), Arg.Any<QrSessionDto>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<QrSessionDto>.Success(call.Arg<QrSessionDto>() with { QrCodeData = "new-qr" }));
        var generated = await f.Generator.Handle(new(f.Order.Id, "MoMo"), default);
        Assert.True(generated.IsSuccess);
        Assert.Equal(40, generated.Value!.Amount);
        Assert.Equal("new-qr", generated.Value.QrCodeData);
        Assert.Null(added!.TransactionRef);
        Assert.StartsWith("POS", added.MerchantReference);
    }

    [Fact]
    public async Task ExpiryCommand_ChangesOnlyExpiredPending_AndEnforcesScope()
    {
        var f = new Fixture(100);
        f.Payment.SetQrMetadata(new QrSessionDto("qr", null, DateTimeOffset.UtcNow.AddSeconds(-1)).ToJson());
        f.Payments.GetTrackedAsync(f.Payment.Id, Arg.Any<CancellationToken>()).Returns(f.Payment);
        var handler = new ExpirePendingPaymentCommandHandler(f.Payments, f.Access, f.Uow, TimeProvider.System, f.Audit);
        f.Access.CanAccessStore(f.Order.StoreId).Returns(false);
        Assert.True((await handler.Handle(new(f.Payment.Id), default)).IsFailure);
        Assert.Equal(PaymentStatus.Pending, f.Payment.Status);
        f.Access.CanAccessStore(f.Order.StoreId).Returns(true);
        Assert.Equal("Timeout", (await handler.Handle(new(f.Payment.Id), default)).Value!.Status);
        Assert.Equal(1, f.Uow.SaveCalls);
    }

    [Fact]
    public void PublicWebhookCommands_HaveNoPermissionContract()
    {
        Assert.False(typeof(IRequirePermission).IsAssignableFrom(typeof(ProcessMomoWebhookCommand)));
        Assert.False(typeof(IRequirePermission).IsAssignableFrom(typeof(ProcessVietQrWebhookCommand)));
        Assert.Equal("payments:create_own", new GenerateQrPaymentCommand(Guid.NewGuid(), "MoMo").RequiredPermission);
    }

    private static void Set(object target, string name, object value) => target.GetType().GetProperty(name)!.SetValue(target, value);

    private sealed class Fixture
    {
        public IOrderRepository Orders { get; } = Substitute.For<IOrderRepository>();
        public IPaymentRepository Payments { get; } = Substitute.For<IPaymentRepository>();
        public IAuditLogRepository Audit { get; } = Substitute.For<IAuditLogRepository>();
        public IRealtimeEventPublisher Realtime { get; } = Substitute.For<IRealtimeEventPublisher>();
        public IPublisher Events { get; } = Substitute.For<IPublisher>();
        public IPaymentGateway Gateway { get; } = Substitute.For<IPaymentGateway>();
        public IStoreAccessPolicy Access { get; } = Substitute.For<IStoreAccessPolicy>();
        public TestUow Uow { get; } = new();
        public Order Order { get; }
        public Payment Payment { get; }
        public Shift Shift { get; }
        public VerifiedWebhookDto Message { get; set; }
        public Result<VerifiedWebhookDto>? VerifyResult { get; set; }
        public QrWebhookProcessor Processor { get; }
        public GenerateQrPaymentCommandHandler Generator { get; }

        public Fixture(decimal amount)
        {
            Order = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            Set(Order, nameof(Order.GrandTotal), 100m);
            Set(Order, nameof(Order.Status), OrderStatus.Confirmed);
            Shift = Shift.Open(Order.StoreId, Order.CreatedBy, 0);
            Set(Order, nameof(Order.ShiftId), Shift.Id);
            Payment = Payment.CreatePendingQr(Order.Id, PaymentMethod.MoMo, amount, "POS0123456789ABCDEF0123",
                new QrSessionDto("qr", null, DateTimeOffset.UtcNow.AddMinutes(15)).ToJson());
            Set(Payment, nameof(Payment.Order), Order);
            Order.Payments.Add(Payment);
            Message = new(Payment.MerchantReference!, "provider-ref", amount, true, DateTimeOffset.UtcNow,
                Order.Id, TimeSpan.FromMinutes(5));
            Gateway.Method.Returns(PaymentMethod.MoMo);
            Gateway.VerifyWebhook(Arg.Any<WebhookEnvelopeDto>()).Returns(_ => VerifyResult ?? Result<VerifiedWebhookDto>.Success(Message));
            Payments.GetForWebhookAsync(PaymentMethod.MoMo, Payment.MerchantReference!, Arg.Any<CancellationToken>()).Returns(Payment);
            Orders.GetByIdWithDetailsAsync(Order.Id, Arg.Any<CancellationToken>()).Returns(Order);
            var shifts = Substitute.For<IShiftRepository>();
            shifts.GetByIdAsync(Order.ShiftId, Arg.Any<CancellationToken>()).Returns(Shift);
            Access.CanAccessStore(Order.StoreId).Returns(true);
            Processor = new([Gateway], Payments, Audit, Uow, Events, Realtime, TimeProvider.System, NullLogger<QrWebhookProcessor>.Instance);
            var user = Substitute.For<ICurrentUser>();
            user.EmployeeId.Returns(Order.CreatedBy);
            Generator = new(Orders, Payments, shifts, Access, Uow, [Gateway], new(), TimeProvider.System, Audit, user);
        }
    }

    private sealed class TestUow : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public bool Committed { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(++SaveCalls);
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
