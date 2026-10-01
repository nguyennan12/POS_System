using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Payments.Errors;
using POS.Application.UseCases.Payments.Queries.GetPaymentStatus;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Tests.Payments;

public class GetPaymentStatusQueryTests
{
    [Theory]
    [InlineData(PaymentStatus.Success)]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Timeout)]
    public async Task Handler_MapsPersistedPaymentStatus(PaymentStatus status)
    {
        var (handler, payments, employee, _) = SetUp();
        var order = Order.CreateDraft(employee.StoreId!.Value, Guid.NewGuid(), employee.Id);
        var payment = new Payment(order.Id, PaymentMethod.Card, 40, "ref", status);
        typeof(Payment).GetProperty(nameof(Payment.Order))!.SetValue(payment, order);
        payments.GetByIdWithOrderAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await handler.Handle(new(payment.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(payment.Id, result.Value!.PaymentId);
        Assert.Equal(order.Id, result.Value.OrderId);
        Assert.Equal("Card", result.Value.Method);
        Assert.Equal(40, result.Value.Amount);
        Assert.Equal(status.ToString(), result.Value.Status);
        Assert.Equal("ref", result.Value.TransactionRef);
        Assert.Equal(payment.PaidAt, result.Value.PaidAt);
    }

    [Fact]
    public async Task Handler_ReturnsNotFoundForMissingPayment()
    {
        var (handler, _, _, _) = SetUp();
        var result = await handler.Handle(new(Guid.NewGuid()), default);
        Assert.Equal(PaymentErrors.PaymentNotFound, result.Error);
    }

    [Fact]
    public async Task Handler_RejectsAnotherStore()
    {
        var (handler, payments, employee, _) = SetUp();
        var order = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        var payment = new Payment(order.Id, PaymentMethod.Card, 40);
        typeof(Payment).GetProperty(nameof(Payment.Order))!.SetValue(payment, order);
        payments.GetByIdWithOrderAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await handler.Handle(new(payment.Id), default);

        Assert.Equal(PaymentErrors.InvalidStore, result.Error);
    }

    [Fact]
    public void Validator_RejectsEmptyIdAndAcceptsValidId()
    {
        var validator = new GetPaymentStatusQueryValidator();
        Assert.False(validator.Validate(new GetPaymentStatusQuery(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new GetPaymentStatusQuery(Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData(false, false, ErrorType.Unauthorized)]
    [InlineData(true, false, ErrorType.Forbidden)]
    [InlineData(true, true, ErrorType.None)]
    public async Task Query_RequiresSeededOrdersReadPermission(
        bool authenticated, bool hasPermission, ErrorType expectedType)
    {
        var user = Substitute.For<ICurrentUser>();
        var employee = new Employee("Employee", "employee", "hash", "hash", Guid.NewGuid());
        user.IsAuthenticated.Returns(authenticated);
        user.EmployeeId.Returns(employee.Id);
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{employee.Id}", Arg.Any<CancellationToken>())
            .Returns(hasPermission ? ["orders:read"] : ["orders:update"]);
        var employees = Substitute.For<IEmployeeRepository>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICacheService)).Returns(cache);
        services.GetService(typeof(IEmployeeRepository)).Returns(employees);
        services.GetService(typeof(IPermissionRepository)).Returns(Substitute.For<IPermissionRepository>());
        var query = new GetPaymentStatusQuery(Guid.NewGuid());
        Assert.Equal("orders:read", ((IRequirePermission)query).RequiredPermission);
        var called = false;

        var result = await new AuthorizationBehavior<GetPaymentStatusQuery, Result<PaymentStatusResponse>>(user, services)
            .Handle(query, () =>
            {
                called = true;
                return Task.FromResult(Result<PaymentStatusResponse>.Success(default!));
            }, default);

        Assert.Equal(authenticated && hasPermission, called);
        if (called) Assert.True(result.IsSuccess);
        else Assert.Equal(expectedType, result.Error.Type);
    }

    private static (GetPaymentStatusQueryHandler Handler, IPaymentRepository Payments,
        Employee Employee, ICurrentUser User) SetUp()
    {
        var payments = Substitute.For<IPaymentRepository>();
        var employees = Substitute.For<IEmployeeRepository>();
        var user = Substitute.For<ICurrentUser>();
        var employee = new Employee("Employee", "employee", "hash", "hash", Guid.NewGuid(),
            storeId: Guid.NewGuid());
        user.EmployeeId.Returns(employee.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        return (new(payments, employees, user), payments, employee, user);
    }
}
