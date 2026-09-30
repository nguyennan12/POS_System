using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Tests.Orders;

public partial class CheckoutAndCancelOrderTests
{
    [Fact]
    public void Checkout_ShouldRequireSeededOrderUpdatePermission()
    {
        var command = new CheckoutOrderCommand(_orderId, [new("Cash", 100)]);

        var permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);
        permissionRequest.RequiredPermission.Should().Be("orders:update");
    }

    [Theory]
    [InlineData(false, false, ErrorType.Unauthorized)]
    [InlineData(true, false, ErrorType.Forbidden)]
    [InlineData(true, true, ErrorType.None)]
    public async Task Checkout_Authorization_ShouldGateHandler(
        bool authenticated, bool hasPermission, ErrorType expectedError)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(authenticated);
        user.EmployeeId.Returns(_employeeId);

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{_employeeId}", Arg.Any<CancellationToken>())
            .Returns(hasPermission ? ["orders:update"] : ["orders:read"]);

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICacheService)).Returns(cache);
        services.GetService(typeof(IEmployeeRepository)).Returns(_employeeRepository);
        services.GetService(typeof(IPermissionRepository)).Returns(Substitute.For<IPermissionRepository>());

        var called = false;
        var command = new CheckoutOrderCommand(_orderId, [new("Cash", 100)]);
        var behavior = new AuthorizationBehavior<CheckoutOrderCommand, Result<CheckoutDto>>(user, services);

        var result = await behavior.Handle(command, () =>
        {
            called = true;
            return Task.FromResult(Result<CheckoutDto>.Success(default!));
        }, default);

        called.Should().Be(hasPermission && authenticated);
        if (hasPermission && authenticated)
            result.IsSuccess.Should().BeTrue();
        else
            result.Error.Type.Should().Be(expectedError);
    }

    private Order SetUpPaymentOrder(decimal due = 500_000)
    {
        var order = CreateDraftOrderWithItems(due);
        _currentUser.EmployeeId.Returns(_employeeId);
        _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    [Theory]
    [InlineData(200_000, 0)]
    [InlineData(250_000, 50_000)]
    public async Task Split_ShouldApplyOnlyRemainingCash_AfterTwoPayments(decimal cash, decimal change)
    {
        var order = SetUpPaymentOrder();
        var handler = CreateCheckoutHandler();
        var first = await handler.Handle(new(_orderId, [new("Card", 100_000, "card-1")]), default);
        first.Value!.TotalPaid.Should().Be(100_000);
        first.Value.Status.Should().Be("Confirmed");
        var second = await handler.Handle(new(_orderId, [new("MoMo", 200_000, "momo-1")]), default);
        second.Value!.TotalPaid.Should().Be(300_000);
        var third = await handler.Handle(new(_orderId, [new("Cash", cash)]), default);

        third.Value!.TotalPaid.Should().Be(500_000);
        third.Value.ChangeAmount.Should().Be(change);
        third.Value.Status.Should().Be("Paid");
        third.Value.ReceiptData!.AmountPaid.Should().Be(500_000);
        order.Payments.Should().HaveCount(3);
        order.GetPaymentTotals().TotalApplied.Should().Be(order.GrandTotal);
    }

    [Fact]
    public async Task Split_ShouldAggregateChangeFromEveryCashPayment()
    {
        SetUpPaymentOrder(100);
        var result = await CreateCheckoutHandler().Handle(new(_orderId,
            [new("Cash", 120), new("Cash", 30)]), default);
        result.Value!.TotalPaid.Should().Be(100);
        result.Value.ChangeAmount.Should().Be(50);
        result.Value.Payments.Select(p => p.ChangeAmount).Should().Equal(20, 30);
    }

    [Theory]
    [InlineData("MoMo")]
    [InlineData("VietQR")]
    [InlineData("Card")]
    [InlineData("Points")]
    public async Task Split_ShouldRejectNonCashAboveRemaining(string method)
    {
        var order = SetUpPaymentOrder(100);
        if (method == "Points") SetUpLoyalty(order, 500);
        order.Confirm();
        order.ProcessPayments([(PaymentMethod.Cash, 60, null)]);
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new(method, 50, "ref")]), default);
        result.Error.Code.Should().Be(OrderErrors.NonCashOverpaymentNotAllowed.Code);
        order.Payments.Should().HaveCount(1);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Split_ShouldRejectBatch_WhenCashLeavesInsufficientRemainingForCard()
    {
        var order = SetUpPaymentOrder(100);
        var result = await CreateCheckoutHandler().Handle(new(_orderId,
            [new("Cash", 80), new("Card", 30, "ref")]), default);
        result.Error.Code.Should().Be(OrderErrors.NonCashOverpaymentNotAllowed.Code);
        order.Payments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Timeout)]
    public async Task Split_ShouldIgnoreUnsuccessfulPayments(PaymentStatus status)
    {
        var order = SetUpPaymentOrder(100);
        order.Payments.Add(new Payment(order.Id, PaymentMethod.Card, 100, "old", status));
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 40)]), default);
        result.Value!.TotalPaid.Should().Be(40);
        result.Value.Status.Should().Be("Confirmed");
        order.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task Split_ShouldRejectDuplicateWithinBatch_UsingCanonicalMethod()
    {
        var order = SetUpPaymentOrder(100);
        var result = await CreateCheckoutHandler().Handle(new(_orderId,
            [new("Card", 20, "ref"), new("card", 20, "ref")]), default);
        result.Error.Code.Should().Be(OrderErrors.DuplicatePayment.Code);
        order.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Split_ShouldAllowSameReferenceForDifferentMethods()
    {
        var order = SetUpPaymentOrder(100);
        var result = await CreateCheckoutHandler().Handle(new(_orderId,
            [new("Card", 40, "ref"), new("MoMo", 60, "ref")]), default);
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task Split_ShouldRejectReferenceAlreadyUsedByAnotherOrder()
    {
        var order = SetUpPaymentOrder(100);
        _orderRepository.PaymentReferenceExistsAsync(PaymentMethod.Card, "ref", Arg.Any<CancellationToken>()).Returns(true);
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new("Card", 40, "ref")]), default);
        result.Error.Code.Should().Be(OrderErrors.DuplicatePayment.Code);
        order.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Split_ShouldRejectRetryWithoutAddingPayment()
    {
        var order = SetUpPaymentOrder(100);
        var handler = CreateCheckoutHandler();
        var command = new CheckoutOrderCommand(_orderId, [new("Card", 40, "ref")]);
        (await handler.Handle(command, default)).IsSuccess.Should().BeTrue();
        (await handler.Handle(command, default)).Error.Code.Should().Be(OrderErrors.DuplicatePayment.Code);
        order.Payments.Should().HaveCount(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Split_ShouldMapDatabaseUniqueConflictToExistingApiConflictConvention()
    {
        SetUpPaymentOrder(100);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<int>(_ =>
            throw new PersistenceConflictException("IX_payments_method_transaction_ref", new Exception()));
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new("Card", 40, "ref")]), default);
        result.Error.Code.Should().Be(OrderErrors.DuplicatePayment.Code);
        result.Error.Type.Should().Be(ErrorType.AlreadyExists);
    }

    private LoyaltyAccount SetUpLoyalty(Order order, decimal balance)
    {
        var customer = new Customer("Customer", "0123456789", Guid.NewGuid());
        order.SetCustomer(customer.Id);
        var account = new LoyaltyAccount(customer.Id, balance);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(_ => new CustomerWithPoints(customer, account.PointsBalance));
        _customerRepository.GetLoyaltyAccountAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(account);
        return account;
    }

    [Fact]
    public async Task Split_ShouldDeductPointsOnPartialPayment_OnlyOnce()
    {
        var order = SetUpPaymentOrder(100);
        var account = SetUpLoyalty(order, 80);
        var handler = CreateCheckoutHandler();
        var command = new CheckoutOrderCommand(_orderId, [new("Points", 30, "points-ref")]);
        var first = await handler.Handle(command, default);
        first.Value!.Status.Should().Be("Confirmed");
        account.PointsBalance.Should().Be(50);
        (await handler.Handle(command, default)).IsFailure.Should().BeTrue();
        account.PointsBalance.Should().Be(50);
        (await handler.Handle(new(_orderId, [new("Cash", 70)]), default)).IsSuccess.Should().BeTrue();
        account.PointsBalance.Should().Be(50);
    }

    [Fact]
    public async Task Split_ShouldValidateCombinedPointsInBatch()
    {
        var order = SetUpPaymentOrder(100);
        var account = SetUpLoyalty(order, 50);
        var result = await CreateCheckoutHandler().Handle(new(_orderId,
            [new("Points", 30), new("Points", 30)]), default);
        result.Error.Code.Should().Be(OrderErrors.InsufficientPoints.Code);
        account.PointsBalance.Should().Be(50);
        order.Payments.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Cash", 0, null, "ORDER.INVALID_PAYMENT_AMOUNT")]
    [InlineData("Cash", -1, null, "ORDER.INVALID_PAYMENT_AMOUNT")]
    [InlineData("Cash", 0.001, null, "ORDER.INVALID_PAYMENT_AMOUNT")]
    [InlineData("Unknown", 1, null, "ORDER.INVALID_PAYMENT_METHOD")]
    [InlineData("999", 1, null, "ORDER.INVALID_PAYMENT_METHOD")]
    [InlineData("Cash", 1, "   ", "ORDER.INVALID_TRANSACTION_REF")]
    public async Task Split_ShouldRejectInvalidInput(string method, decimal amount, string? reference, string code)
    {
        var order = SetUpPaymentOrder(100);
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new(method, amount, reference)]), default);
        result.Error.Code.Should().Be(code);
        order.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Split_ShouldRejectInvalidStoredPaymentState()
    {
        var order = SetUpPaymentOrder(100);
        order.Payments.Add(new Payment(order.Id, PaymentMethod.Card, 20, status: (PaymentStatus)99));
        var result = await CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 80)]), default);
        result.Error.Code.Should().Be(OrderErrors.InvalidPaymentState.Code);
    }

    [Fact]
    public async Task Split_ShouldReadOrderAndValidateInsideUnitOfWork()
    {
        SetUpPaymentOrder(100);
        var inTransaction = false;
        _unitOfWork.ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<Result<CheckoutDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                inTransaction = true;
                try { return await call.Arg<Func<CancellationToken, Task<Result<CheckoutDto>>>>()(default); }
                finally { inTransaction = false; }
            });
        _orderRepository.When(x => x.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()))
            .Do(_ => inTransaction.Should().BeTrue());
        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => inTransaction.Should().BeTrue());
        (await CreateCheckoutHandler().Handle(new(_orderId, [new("Cash", 100)]), default)).IsSuccess.Should().BeTrue();
        await _orderRepository.Received(1).GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>());
    }
}
