using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Payments;
using POS.Application.UseCases.Payments.Strategies;
using POS.Domain.Customers;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Stores;

namespace POS.Application.Tests.Orders;

public class PaymentStrategyTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly Order _dummyOrder;

    public PaymentStrategyTests()
    {
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        _dummyOrder = Order.CreateDraft(storeId, shiftId, employeeId);
    }

    [Fact]
    public async Task CashPaymentStrategy_ShouldSucceed_WhenAmountPositive()
    {
        var strategy = new CashPaymentStrategy();
        strategy.Method.Should().Be(PaymentMethod.Cash);

        var payment = new PaymentSplitInputDto("Cash", 100_000);
        var result = await strategy.ValidateAsync(payment, _dummyOrder);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CashPaymentStrategy_ShouldFail_WhenAmountZeroOrNegative()
    {
        var strategy = new CashPaymentStrategy();
        var payment = new PaymentSplitInputDto("Cash", 0);
        var result = await strategy.ValidateAsync(payment, _dummyOrder);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.InvalidPaymentAmount.Code);
    }

    [Theory]
    [InlineData("MoMo", PaymentMethod.MoMo)]
    [InlineData("VietQR", PaymentMethod.VietQR)]
    [InlineData("Card", PaymentMethod.Card)]
    public async Task ElectronicPaymentStrategies_ShouldFail_WhenTransactionRefMissing(string methodStr, PaymentMethod expectedMethod)
    {
        IPaymentStrategy strategy = expectedMethod switch
        {
            PaymentMethod.MoMo => new MoMoPaymentStrategy(),
            PaymentMethod.VietQR => new VietQrPaymentStrategy(),
            PaymentMethod.Card => new CardPaymentStrategy(),
            _ => throw new ArgumentException()
        };

        strategy.Method.Should().Be(expectedMethod);

        var payment = new PaymentSplitInputDto(methodStr, 50_000, null);
        var result = await strategy.ValidateAsync(payment, _dummyOrder);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.TransactionRefRequired.Code);
    }

    [Fact]
    public async Task PointsPaymentStrategy_ShouldFail_WhenOrderHasNoCustomer()
    {
        var strategy = new PointsPaymentStrategy(_customerRepository);
        strategy.Method.Should().Be(PaymentMethod.Points);

        var payment = new PaymentSplitInputDto("Points", 50_000);
        var result = await strategy.ValidateAsync(payment, _dummyOrder);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.PointsRequireCustomer.Code);
    }

    [Fact]
    public async Task PointsPaymentStrategy_ShouldFail_WhenCustomerPointsInsufficient()
    {
        var customerId = Guid.NewGuid();
        _dummyOrder.SetCustomer(customerId);

        var customer = new Customer("Khách B", "0911223344", Guid.NewGuid(), null, null, null, true, customerId);
        _customerRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new CustomerWithPoints(customer, PointsBalance: 10_000));

        var strategy = new PointsPaymentStrategy(_customerRepository);
        var payment = new PaymentSplitInputDto("Points", 50_000);
        var result = await strategy.ValidateAsync(payment, _dummyOrder);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.InsufficientPoints.Code);
    }

    [Fact]
    public async Task PointsPaymentStrategy_ShouldSucceed_WhenCustomerHasEnoughPoints()
    {
        var customerId = Guid.NewGuid();
        _dummyOrder.SetCustomer(customerId);

        var customer = new Customer("Khách B", "0911223344", Guid.NewGuid(), null, null, null, true, customerId);
        _customerRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new CustomerWithPoints(customer, PointsBalance: 100_000));

        var strategy = new PointsPaymentStrategy(_customerRepository);
        var payment = new PaymentSplitInputDto("Points", 50_000);
        var result = await strategy.ValidateAsync(payment, _dummyOrder);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PaymentStrategyFactory_ShouldReturnCorrectStrategy_ForEachMethod()
    {
        var strategies = new IPaymentStrategy[]
        {
            new CashPaymentStrategy(),
            new MoMoPaymentStrategy(),
            new VietQrPaymentStrategy(),
            new CardPaymentStrategy(),
            new PointsPaymentStrategy(_customerRepository)
        };

        var factory = new PaymentStrategyFactory(strategies);

        factory.GetStrategy(PaymentMethod.Cash).Should().BeOfType<CashPaymentStrategy>();
        factory.GetStrategy(PaymentMethod.MoMo).Should().BeOfType<MoMoPaymentStrategy>();
        factory.GetStrategy(PaymentMethod.VietQR).Should().BeOfType<VietQrPaymentStrategy>();
        factory.GetStrategy(PaymentMethod.Card).Should().BeOfType<CardPaymentStrategy>();
        factory.GetStrategy(PaymentMethod.Points).Should().BeOfType<PointsPaymentStrategy>();
    }

    [Fact]
    public void PaymentStrategyFactory_ParseMethod_ShouldValidateCorrectly()
    {
        var factory = new PaymentStrategyFactory([]);

        var validResult = factory.ParseMethod("momo");
        validResult.IsSuccess.Should().BeTrue();
        validResult.Value.Should().Be(PaymentMethod.MoMo);

        var invalidResult = factory.ParseMethod("InvalidMethod");
        invalidResult.IsFailure.Should().BeTrue();
        invalidResult.Error.Code.Should().Be(OrderErrors.InvalidPaymentMethod.Code);
    }
}
