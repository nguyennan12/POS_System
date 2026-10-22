using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Payments;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Errors;
using POS.Application.UseCases.Payments.Queries.AuthorizePaymentSubscription;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;

namespace POS.Application.Tests.Payments;

public class AuthorizePaymentSubscriptionQueryTests
{
    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Success)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Timeout)]
    public async Task OwnStoreAndShift_ReturnsPersistedSnapshotWithoutChangingState(PaymentStatus status)
    {
        var f = new Fixture(status);
        var result = await f.Read();
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatusDto.From(f.Payment), result.Value);
        Assert.Equal(status, f.Payment.Status);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("missing-employee")]
    [InlineData("inactive")]
    [InlineData("locked")]
    public async Task InvalidIdentity_IsRejectedBeforeReadingPayment(string reason)
    {
        var f = new Fixture();
        switch (reason)
        {
            case "anonymous": f.User.IsAuthenticated.Returns(false); break;
            case "missing-employee": f.Employees.GetByIdAsync(f.Employee.Id, Arg.Any<CancellationToken>()).Returns((Employee?)null); break;
            case "inactive": f.Employee.SetActive(false); break;
            case "locked": f.Employee.RegisterFailedLogin(DateTime.UtcNow, maxAttempts: 1); break;
        }
        Assert.Equal(PaymentErrors.Unauthorized, (await f.Read()).Error);
        await f.Payments.DidNotReceive().GetByIdWithOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnotherStore_IsDeniedEvenForTheOrderCreator()
    {
        var f = new Fixture();
        f.Employee.UpdateProfile(f.Employee.Name, f.Employee.RoleId, Guid.NewGuid(), false);
        Assert.Equal(PaymentErrors.InvalidStore, (await f.Read()).Error);
    }

    [Fact]
    public async Task MissingPayment_ReturnsNotFound()
    {
        var f = new Fixture();
        f.Payments.GetByIdWithOrderAsync(f.Payment.Id, Arg.Any<CancellationToken>()).Returns((Payment?)null);
        Assert.Equal(PaymentErrors.PaymentNotFound, (await f.Read()).Error);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("other-store")]
    [InlineData("other-shift")]
    [InlineData("other-cashier")]
    public async Task InvalidShift_IsDenied(string reason)
    {
        var f = new Fixture();
        Shift? shift = reason switch
        {
            "missing" => null,
            "other-store" => Shift.Open(Guid.NewGuid(), f.Employee.Id, 0),
            "other-shift" => Shift.Open(f.Order.StoreId, f.Employee.Id, 0),
            _ => Shift.Open(f.Order.StoreId, Guid.NewGuid(), 0)
        };
        if (shift is not null && reason != "other-shift") Set(f.Order, nameof(Order.ShiftId), shift.Id);
        f.Shifts.GetByIdAsync(f.Order.ShiftId, Arg.Any<CancellationToken>()).Returns(shift);
        Assert.Equal(PaymentErrors.InvalidShift, (await f.Read()).Error);
    }

    [Theory]
    [InlineData(RoleNames.StoreManager, true, true)]
    [InlineData(RoleNames.Owner, true, true)]
    [InlineData(RoleNames.Cashier, true, false)]
    [InlineData(RoleNames.StoreManager, false, false)]
    public async Task CreatorMaySubscribeOnlyWhenSystemManagerOrOwnerOpenedShift(string roleName, bool system, bool allowed)
    {
        var f = new Fixture();
        var role = new Role(roleName, isSystemRole: system);
        var opener = new Employee("Opener", "opener", "hash", "hash", role.Id, storeId: f.Order.StoreId);
        Set(opener, nameof(Employee.Role), role);
        var shift = Shift.Open(f.Order.StoreId, opener.Id, 0);
        Set(f.Order, nameof(Order.ShiftId), shift.Id);
        f.Shifts.GetByIdAsync(f.Order.ShiftId, Arg.Any<CancellationToken>()).Returns(shift);
        f.Employees.GetByIdAsync(opener.Id, Arg.Any<CancellationToken>()).Returns(opener);

        Assert.Equal(allowed, (await f.Read()).IsSuccess);
        Set(f.Order, nameof(Order.CreatedBy), Guid.NewGuid());
        Assert.Equal(PaymentErrors.InvalidShift, (await f.Read()).Error);
    }

    [Theory]
    [InlineData(PaymentStatus.Success, true)]
    [InlineData(PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Timeout, true)]
    [InlineData(PaymentStatus.Pending, false)]
    public async Task ClosedShift_AllowsOnlyTerminalSnapshots(PaymentStatus status, bool allowed)
    {
        var f = new Fixture(status);
        f.Shift.Close(0, 0, null);
        Assert.Equal(allowed, (await f.Read()).IsSuccess);
    }

    [Theory]
    [InlineData(false, false, ErrorType.Unauthorized)]
    [InlineData(true, false, ErrorType.Forbidden)]
    [InlineData(true, true, ErrorType.None)]
    public async Task Pipeline_RequiresPaymentsRead(bool authenticated, bool permitted, ErrorType expected)
    {
        var f = new Fixture();
        f.User.IsAuthenticated.Returns(authenticated);
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{f.Employee.Id}", Arg.Any<CancellationToken>())
            .Returns(permitted ? ["payments:read"] : ["payments:create_own"]);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICacheService)).Returns(cache);
        services.GetService(typeof(IEmployeeRepository)).Returns(f.Employees);
        services.GetService(typeof(IPermissionRepository)).Returns(Substitute.For<IPermissionRepository>());
        var called = false;
        var result = await new AuthorizationBehavior<AuthorizePaymentSubscriptionQuery, Result<PaymentStatusDto>>(f.User, services)
            .Handle(new(f.Payment.Id), async () => { called = true; return await f.Read(); }, default);
        Assert.Equal(authenticated && permitted, called);
        Assert.Equal(expected, result.IsSuccess ? ErrorType.None : result.Error.Type);
    }

    [Fact]
    public void Validator_RejectsEmptyPaymentId()
    {
        var validator = new AuthorizePaymentSubscriptionQueryValidator();
        Assert.False(validator.Validate(new AuthorizePaymentSubscriptionQuery(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new AuthorizePaymentSubscriptionQuery(Guid.NewGuid())).IsValid);
    }

    private static void Set(object target, string property, object value) => target.GetType().GetProperty(property)!.SetValue(target, value);

    private sealed class Fixture
    {
        public IPaymentRepository Payments { get; } = Substitute.For<IPaymentRepository>();
        public IEmployeeRepository Employees { get; } = Substitute.For<IEmployeeRepository>();
        public IShiftRepository Shifts { get; } = Substitute.For<IShiftRepository>();
        public ICurrentUser User { get; } = Substitute.For<ICurrentUser>();
        public Employee Employee { get; } = new("Cashier", "cashier", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());
        public Order Order { get; }
        public Payment Payment { get; }
        public Shift Shift { get; }
        private readonly AuthorizePaymentSubscriptionQueryHandler handler;

        public Fixture(PaymentStatus status = PaymentStatus.Pending)
        {
            Shift = Shift.Open(Employee.StoreId!.Value, Employee.Id, 0);
            Order = Order.CreateDraft(Employee.StoreId.Value, Shift.Id, Employee.Id);
            Payment = new Payment(Order.Id, PaymentMethod.MoMo, 40, status: status);
            Set(Payment, nameof(Payment.Order), Order);
            User.IsAuthenticated.Returns(true);
            User.EmployeeId.Returns(Employee.Id);
            Employees.GetByIdAsync(Employee.Id, Arg.Any<CancellationToken>()).Returns(Employee);
            Payments.GetByIdWithOrderAsync(Payment.Id, Arg.Any<CancellationToken>()).Returns(Payment);
            Shifts.GetByIdAsync(Shift.Id, Arg.Any<CancellationToken>()).Returns(Shift);
            handler = new(Payments, Employees, Shifts, User, new PaymentSubscriptionAccessPolicy(Employees), TimeProvider.System);
        }

        public Task<Result<PaymentStatusDto>> Read() => handler.Handle(new(Payment.Id), default);
    }
}
