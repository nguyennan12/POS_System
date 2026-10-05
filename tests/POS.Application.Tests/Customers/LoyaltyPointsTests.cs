using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Customers.Commands.AccruePoints;
using POS.Application.UseCases.Customers.Commands.AdjustPoints;
using POS.Application.UseCases.Customers.Commands.RedeemPoints;
using POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;
using POS.Application.UseCases.Customers.Queries.GetPointTransactions;
using POS.Application.UseCases.Customers;
using POS.Domain.Common;
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

  public LoyaltyPointsTests()
  {
    unitOfWork.ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<Result<LoyaltyAccountDto>>>>(), Arg.Any<CancellationToken>())
        .Returns(call => call.Arg<Func<CancellationToken, Task<Result<LoyaltyAccountDto>>>>()(call.Arg<CancellationToken>()));
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task MissingAccount_ShouldAddOnlyAccount(bool adjust)
  {
    var customer = new Customer("Customer", "0901234567", defaultTier.Id);
    customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
    customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>())
        .Returns((LoyaltyAccount?)null);

    var result = adjust
        ? await new AdjustPointsCommandHandler(customerRepository, unitOfWork)
            .Handle(new(customer.Id, 25, "Adjustment"), CancellationToken.None)
        : await new AccruePointsCommandHandler(customerRepository, unitOfWork)
            .Handle(new(customer.Id, 25), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value!.PointsBalance.Should().Be(25);
    await customerRepository.Received(1).AddLoyaltyAccountAsync(
        Arg.Is<LoyaltyAccount>(a => a.CustomerId == customer.Id && a.PointsBalance == 25), Arg.Any<CancellationToken>());
    await customerRepository.DidNotReceive().AddAsync(
        Arg.Any<Customer>(), Arg.Any<LoyaltyAccount>(), Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(false, 20, false)]
  [InlineData(true, 20, false)]
  [InlineData(false, 80, true)]
  [InlineData(true, 80, true)]
  public async Task SerializationConflict_ShouldReloadAndRecheckBalance(bool adjust, int freshBalance, bool succeeds)
  {
    var customer = new Customer("Customer", "0901234567", defaultTier.Id);
    customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
    using var cancellation = new CancellationTokenSource();
    var attempts = 0;
    var inTransaction = false;
    customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, Arg.Any<CancellationToken>())
        .Returns(_ =>
        {
          inTransaction.Should().BeTrue();
          return new LoyaltyAccount(customer.Id, attempts == 1 ? 100 : freshBalance);
        });
    unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
    {
      inTransaction.Should().BeTrue();
      return 1;
    });
    unitOfWork.ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<Result<LoyaltyAccountDto>>>>(), cancellation.Token)
        .Returns(async call =>
        {
          attempts++;
          inTransaction = true;
          var result = await call.Arg<Func<CancellationToken, Task<Result<LoyaltyAccountDto>>>>()(cancellation.Token);
          inTransaction = false;
          // Simulate the first transaction losing a race at commit. The next read
          // returns the freshly committed balance, as UnitOfWork clears tracking.
          return attempts == 1
                  ? Result<LoyaltyAccountDto>.Failure(new Error(ErrorType.Invalid, "Persistence.ConcurrentModification", "Conflict"))
                  : result;
        });

    var result = adjust
        ? await new AdjustPointsCommandHandler(customerRepository, unitOfWork)
            .Handle(new(customer.Id, -40, "Adjustment"), cancellation.Token)
        : await new RedeemPointsCommandHandler(customerRepository, unitOfWork)
            .Handle(new(customer.Id, 40), cancellation.Token);

    attempts.Should().Be(2);
    result.IsSuccess.Should().Be(succeeds);
    if (succeeds)
      result.Value!.PointsBalance.Should().Be(freshBalance - 40);
    else
      result.Error.Should().Be(CustomerErrors.InsufficientPoints);
    await customerRepository.Received(2).GetEntityByIdAsync(customer.Id, cancellation.Token);
    await customerRepository.Received(2).GetLoyaltyAccountWithTierAsync(customer.Id, cancellation.Token);
    await unitOfWork.Received(succeeds ? 2 : 1).SaveChangesAsync(cancellation.Token);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task RepeatedSerializationConflict_ShouldStopAfterThreeAttempts(bool adjust)
  {
    var conflict = new Error(ErrorType.Invalid, "Persistence.ConcurrentModification", "Conflict");
    unitOfWork.ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<Result<LoyaltyAccountDto>>>>(), Arg.Any<CancellationToken>())
        .Returns(Result<LoyaltyAccountDto>.Failure(conflict));

    var result = adjust
        ? await new AdjustPointsCommandHandler(customerRepository, unitOfWork)
            .Handle(new(Guid.NewGuid(), -40, "Adjustment"), CancellationToken.None)
        : await new RedeemPointsCommandHandler(customerRepository, unitOfWork)
            .Handle(new(Guid.NewGuid(), 40), CancellationToken.None);

    result.Error.Should().Be(conflict);
    await unitOfWork.Received(3).ExecuteSerializableAsync(
        Arg.Any<Func<CancellationToken, Task<Result<LoyaltyAccountDto>>>>(), Arg.Any<CancellationToken>());
  }


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
