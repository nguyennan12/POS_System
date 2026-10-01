using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Customers.Commands.AccruePoints;
using POS.Application.UseCases.Customers.Commands.AdjustPoints;
using POS.Application.UseCases.Customers.Commands.RedeemPoints;
using POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;
using POS.Application.UseCases.Customers.Queries.GetPointTransactions;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;
using Xunit;

namespace POS.Application.Tests.Customers;

public class LoyaltyPointsTests
{
    private readonly ICustomerRepository customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly MemberTier defaultTier = new(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");

    /// <summary>
    /// Verifies that accrual increases the balance and saves an earn transaction with its note.
    /// </summary>
    [Fact]
    public async Task AccruePoints_Valid_ShouldIncreaseBalanceAndRecordPointTransaction()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 100);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new AccruePointsCommandHandler(customerRepository, unitOfWork);
        var command = new AccruePointsCommand(customer.Id, 50, null, "Tích điểm mua hàng");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointsBalance.Should().Be(150);
        loyaltyAccount.PointsBalance.Should().Be(150);

        await customerRepository.Received(1).AddPointTransactionAsync(
            Arg.Is<PointTransaction>(t => t.CustomerId == customer.Id && t.Points == 50 && t.Type == PointTransactionType.Earn && t.Note == "Tích điểm mua hàng"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that accrual for a missing customer returns a not-found error without saving changes.
    /// </summary>
    [Fact]
    public async Task AccruePoints_WhenCustomerNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        customerRepository.GetEntityByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var handler = new AccruePointsCommandHandler(customerRepository, unitOfWork);
        var command = new AccruePointsCommand(customerId, 50);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.NotFound);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that accrual for an inactive customer returns an inactive error without saving changes.
    /// </summary>
    [Fact]
    public async Task AccruePoints_WhenCustomerInactive_ShouldReturnInactiveError()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        customer.Deactivate();

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var handler = new AccruePointsCommandHandler(customerRepository, unitOfWork);
        var command = new AccruePointsCommand(customer.Id, 50);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.Inactive);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that redemption decreases the balance and saves a redeem transaction with its note.
    /// </summary>
    [Fact]
    public async Task RedeemPoints_Valid_ShouldDecreaseBalanceAndRecordPointTransaction()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 100);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new RedeemPointsCommandHandler(customerRepository, unitOfWork);
        var command = new RedeemPointsCommand(customer.Id, 40, null, "Đổi điểm giảm giá");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointsBalance.Should().Be(60);
        loyaltyAccount.PointsBalance.Should().Be(60);

        await customerRepository.Received(1).AddPointTransactionAsync(
            Arg.Is<PointTransaction>(t => t.CustomerId == customer.Id && t.Points == 40 && t.Type == PointTransactionType.Redeem && t.Note == "Đổi điểm giảm giá"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that redemption exceeding the balance preserves points and returns an insufficient-points error without saving.
    /// </summary>
    [Fact]
    public async Task RedeemPoints_WhenPointsExceedBalance_ShouldReturnInsufficientPointsError()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 50);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new RedeemPointsCommandHandler(customerRepository, unitOfWork);
        var command = new RedeemPointsCommand(customer.Id, 100);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.InsufficientPoints);
        loyaltyAccount.PointsBalance.Should().Be(50); // Points balance preserved >= 0
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a positive adjustment increases the balance and saves an adjustment transaction.
    /// </summary>
    [Fact]
    public async Task AdjustPoints_Positive_ShouldIncreaseBalanceAndRecordPointTransaction()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 50);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new AdjustPointsCommandHandler(customerRepository, unitOfWork);
        var command = new AdjustPointsCommand(customer.Id, 30, "Bồi thường khiếu nại");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointsBalance.Should().Be(80);
        loyaltyAccount.PointsBalance.Should().Be(80);

        await customerRepository.Received(1).AddPointTransactionAsync(
            Arg.Is<PointTransaction>(t => t.CustomerId == customer.Id && t.Points == 30 && t.Type == PointTransactionType.Adjust && t.Note == "Bồi thường khiếu nại"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a negative adjustment decreases the balance and saves the signed adjustment transaction.
    /// </summary>
    [Fact]
    public async Task AdjustPoints_Negative_ShouldDecreaseBalanceAndRecordPointTransaction()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 50);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new AdjustPointsCommandHandler(customerRepository, unitOfWork);
        var command = new AdjustPointsCommand(customer.Id, -20, "Trừ điểm do hoàn đơn");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointsBalance.Should().Be(30);
        loyaltyAccount.PointsBalance.Should().Be(30);

        await customerRepository.Received(1).AddPointTransactionAsync(
            Arg.Is<PointTransaction>(t => t.CustomerId == customer.Id && t.Points == -20 && t.Type == PointTransactionType.Adjust && t.Note == "Trừ điểm do hoàn đơn"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that an excessive negative adjustment preserves the balance and returns an error without saving.
    /// </summary>
    [Fact]
    public async Task AdjustPoints_NegativeExceedingBalance_ShouldReturnInsufficientPointsError()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 30);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new AdjustPointsCommandHandler(customerRepository, unitOfWork);
        var command = new AdjustPointsCommand(customer.Id, -50, "Trừ điểm quá mức");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.InsufficientPoints);
        loyaltyAccount.PointsBalance.Should().Be(30); // Points balance preserved >= 0
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the loyalty query returns the requested customer and point balance.
    /// </summary>
    [Fact]
    public async Task GetLoyaltyAccount_Valid_ShouldReturnAccountDetailsAndTier()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        var loyaltyAccount = new LoyaltyAccount(customer.Id, 120);

        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);

        var handler = new GetLoyaltyAccountQueryHandler(customerRepository);
        var query = new GetLoyaltyAccountQuery(customer.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerId.Should().Be(customer.Id);
        result.Value.PointsBalance.Should().Be(120);
    }

    /// <summary>
    /// Verifies that the transaction query returns the expected count, items, and transaction type names.
    /// </summary>
    [Fact]
    public async Task GetPointTransactions_Valid_ShouldReturnPagedTransactions()
    {
        // Arrange
        var customer = new Customer("Nguyen Van A", "0901234567", defaultTier.Id);
        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var tx1 = new PointTransaction(customer.Id, 50, PointTransactionType.Earn, null, "Tích điểm");
        var tx2 = new PointTransaction(customer.Id, -20, PointTransactionType.Adjust, null, "Điều chỉnh");

        customerRepository.GetPointTransactionsPagedAsync(
            customer.Id, Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<PointTransactionType?>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns(([tx1, tx2], 2));

        var handler = new GetPointTransactionsQueryHandler(customerRepository);
        var query = new GetPointTransactionsQuery(customer.Id, null, null, null, 1, 20);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Type.Should().Be("Earn");
        result.Value.Items[1].Type.Should().Be("Adjust");
    }
}
