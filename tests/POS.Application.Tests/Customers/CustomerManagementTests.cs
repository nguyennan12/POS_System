using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Customers.Commands.CreateCustomer;
using POS.Application.UseCases.Customers.Commands.DeleteCustomer;
using POS.Application.UseCases.Customers.Commands.UpdateCustomer;
using POS.Application.UseCases.Customers.Commands.UpdateMemberTier;
using POS.Application.UseCases.Customers.Queries.GetCustomerById;
using POS.Application.UseCases.Customers.Queries.GetCustomers;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;
using Xunit;

namespace POS.Application.Tests.Customers;

public class CustomerManagementTests
{
    private readonly ICustomerRepository customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IMemberTierRepository memberTierRepository = Substitute.For<IMemberTierRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly MemberTier defaultTier = new(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");

    public CustomerManagementTests()
    {
        memberTierRepository.GetDefaultTierAsync(Arg.Any<CancellationToken>()).Returns(defaultTier);
        memberTierRepository.GetByIdAsync(defaultTier.Id, Arg.Any<CancellationToken>()).Returns(defaultTier);
    }

    [Fact]
    public async Task CreateCustomer_Success_ShouldAddCustomerAndLoyaltyAccount()
    {
        // Arrange
        customerRepository.IsPhoneUniqueAsync("0901234567", null, Arg.Any<CancellationToken>()).Returns(true);
        customerRepository.IsBarcodeUniqueAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateCustomerCommandHandler(customerRepository, memberTierRepository, unitOfWork);
        var command = new CreateCustomerCommand("Nguyen Van A", "0901234567", "a@example.com", new DateOnly(1990, 1, 1), "BAR-001");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Nguyen Van A");
        result.Value.Phone.Should().Be("0901234567");
        result.Value.MemberTierName.Should().Be("Normal");
        result.Value.PointsBalance.Should().Be(0);

        await customerRepository.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.Phone == "0901234567" && c.MemberTierId == defaultTier.Id),
            Arg.Is<LoyaltyAccount>(l => l.PointsBalance == 0),
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomer_DuplicatePhone_ShouldReturnError()
    {
        // Arrange
        customerRepository.IsPhoneUniqueAsync("0901234567", null, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateCustomerCommandHandler(customerRepository, memberTierRepository, unitOfWork);
        var command = new CreateCustomerCommand("Nguyen Van A", "0901234567");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.DuplicatePhone);
        await customerRepository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<LoyaltyAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomer_DuplicateBarcode_ShouldReturnError()
    {
        // Arrange
        customerRepository.IsPhoneUniqueAsync("0901234567", null, Arg.Any<CancellationToken>()).Returns(true);
        customerRepository.IsBarcodeUniqueAsync("BAR-001", null, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateCustomerCommandHandler(customerRepository, memberTierRepository, unitOfWork);
        var command = new CreateCustomerCommand("Nguyen Van A", "0901234567", Barcode: "BAR-001");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.DuplicateBarcode);
    }

    [Fact]
    public async Task UpdateCustomer_Success_ShouldUpdateDetails()
    {
        // Arrange
        var customer = new Customer("Old Name", "0901234567", defaultTier.Id);
        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.IsPhoneUniqueAsync("0988888888", customer.Id, Arg.Any<CancellationToken>()).Returns(true);
        customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
            .Returns(new CustomerWithPoints(customer, 50));

        var handler = new UpdateCustomerCommandHandler(customerRepository, memberTierRepository, unitOfWork);
        var command = new UpdateCustomerCommand(customer.Id, "New Name", "0988888888", "new@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        customer.Name.Should().Be("New Name");
        customer.Phone.Should().Be("0988888888");
        customer.Email.Should().Be("new@example.com");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCustomer_WhenHasOrders_ShouldDeactivateCustomer()
    {
        // Arrange
        var customer = new Customer("Customer", "0901234567", defaultTier.Id);
        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.HasOrdersAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteCustomerCommandHandler(customerRepository, unitOfWork);

        // Act
        var result = await handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        customer.IsActive.Should().BeFalse();
        customerRepository.DidNotReceive().Remove(Arg.Any<Customer>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCustomer_WhenNoOrders_ShouldRemoveCustomer()
    {
        // Arrange
        var customer = new Customer("Customer", "0901234567", defaultTier.Id);
        customerRepository.GetEntityByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        customerRepository.HasOrdersAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteCustomerCommandHandler(customerRepository, unitOfWork);

        // Act
        var result = await handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        customerRepository.Received(1).Remove(customer);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMemberTier_ValidOrder_ShouldSucceed()
    {
        // Arrange
        var normal = new MemberTier(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");
        var silver = new MemberTier(MemberTierName.Silver, 5000000m, 0.015m, 0.02m, "#C0C0C0");
        var gold = new MemberTier(MemberTierName.Gold, 15000000m, 0.02m, 0.05m, "#FFD700");
        var vip = new MemberTier(MemberTierName.VIP, 30000000m, 0.03m, 0.10m, "#9400D3");
        var allTiers = new List<MemberTier> { normal, silver, gold, vip };

        memberTierRepository.GetByIdAsync(silver.Id, Arg.Any<CancellationToken>()).Returns(silver);
        memberTierRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allTiers);

        var handler = new UpdateMemberTierCommandHandler(memberTierRepository, unitOfWork);
        // Update silver from 5M to 7M (still > 0 and < 15M)
        var command = new UpdateMemberTierCommand(silver.Id, 7000000m, 0.015m, 0.02m, "#C0C0C0");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        silver.MinSpending.Should().Be(7000000m);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMemberTier_DuplicateMinSpending_ShouldReturnConflict()
    {
        // Arrange
        var normal = new MemberTier(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");
        var silver = new MemberTier(MemberTierName.Silver, 5000000m, 0.015m, 0.02m, "#C0C0C0");
        var gold = new MemberTier(MemberTierName.Gold, 15000000m, 0.02m, 0.05m, "#FFD700");
        var vip = new MemberTier(MemberTierName.VIP, 30000000m, 0.03m, 0.10m, "#9400D3");
        var allTiers = new List<MemberTier> { normal, silver, gold, vip };

        memberTierRepository.GetByIdAsync(silver.Id, Arg.Any<CancellationToken>()).Returns(silver);
        memberTierRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allTiers);

        var handler = new UpdateMemberTierCommandHandler(memberTierRepository, unitOfWork);
        // Setting silver minSpending to 15M (same as gold)
        var command = new UpdateMemberTierCommand(silver.Id, 15000000m, 0.015m, 0.02m, "#C0C0C0");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.MemberTierDuplicateMinSpending);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMemberTier_InvalidOrder_ShouldReturnValidationError()
    {
        // Arrange
        var normal = new MemberTier(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");
        var silver = new MemberTier(MemberTierName.Silver, 5000000m, 0.015m, 0.02m, "#C0C0C0");
        var gold = new MemberTier(MemberTierName.Gold, 15000000m, 0.02m, 0.05m, "#FFD700");
        var vip = new MemberTier(MemberTierName.VIP, 30000000m, 0.03m, 0.10m, "#9400D3");
        var allTiers = new List<MemberTier> { normal, silver, gold, vip };

        memberTierRepository.GetByIdAsync(silver.Id, Arg.Any<CancellationToken>()).Returns(silver);
        memberTierRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allTiers);

        var handler = new UpdateMemberTierCommandHandler(memberTierRepository, unitOfWork);
        // Setting silver minSpending to 20M (> gold 15M, breaking Normal < Silver < Gold < VIP order)
        var command = new UpdateMemberTierCommand(silver.Id, 20000000m, 0.015m, 0.02m, "#C0C0C0");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.MemberTierInvalidOrder);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
