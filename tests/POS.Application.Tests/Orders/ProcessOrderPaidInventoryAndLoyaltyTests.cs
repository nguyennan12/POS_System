using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.ProcessOrderPaid;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Stores;
using Xunit;

namespace POS.Application.Tests.Orders;

public class ProcessOrderPaidInventoryAndLoyaltyTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IStockEntryRepository _stockEntryRepository = Substitute.For<IStockEntryRepository>();
    private readonly IStockTransactionRepository _stockTransactionRepository = Substitute.For<IStockTransactionRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IMemberTierRepository _memberTierRepository = Substitute.For<IMemberTierRepository>();

    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _shiftId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    private readonly MemberTier _normalTier = new(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");
    private readonly MemberTier _silverTier = new(MemberTierName.Silver, 1_000_000m, 0.02m, 0.05m, "#C0C0C0");
    private readonly MemberTier _goldTier = new(MemberTierName.Gold, 5_000_000m, 0.03m, 0.10m, "#FFD700");

    private ProcessOrderPaidCommandHandler CreateHandler() => new(
        _orderRepository,
        _stockEntryRepository,
        _stockTransactionRepository,
        _customerRepository,
        _memberTierRepository,
        NullLogger<ProcessOrderPaidCommandHandler>.Instance);

    private Order CreatePaidOrder(Guid? customerId = null, decimal itemPrice = 100_000m, decimal qty = 2m)
    {
        var order = Order.CreateDraft(_storeId, _shiftId, _employeeId, customerId, "VND", null, _orderId);
        var product = new Product(_storeId, Guid.NewGuid(), "Trà sữa Ô Long", "Ly");
        var sku = new Sku(product.Id, _storeId, "SKU-TEA", "8930001", itemPrice, itemPrice / 2, 0, true)
        {
            Product = product
        };

        order.AddOrUpdateItem(sku, qty);
        var subtotal = itemPrice * qty;
        order.ApplyPromotionEvaluation(new PromotionResult(subtotal, 0, subtotal, [], [], []), new Dictionary<Guid, decimal>());
        order.Confirm();
        order.ProcessPayments([(PaymentMethod.Cash, subtotal, null)]);
        order.AggregatePaymentStatus();
        return order;
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFound);
    }

    [Fact]
    public async Task Handle_WhenOrderNotPaid_ShouldReturnOrderNotPaidError()
    {
        // Arrange: order is Draft
        var order = Order.CreateDraft(_storeId, _shiftId, _employeeId, null, "VND", null, _orderId);
        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotPaid);
    }

    [Fact]
    public async Task Handle_WhenOrderPaid_ShouldDeductStockAndCreateStockTransactionSaleOut()
    {
        // Arrange
        var order = CreatePaidOrder(qty: 3);
        var item = order.Items.First();
        var stockEntry = new StockEntry(_storeId, item.SkuId, qtyOnHand: 10, minStock: 2);
        stockEntry.IncreaseStock(10, 50_000); // Set AverageCost

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>()).Returns(stockEntry);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sideEffects = result.Value!;
        sideEffects.StockTransactions.Should().HaveCount(1);
        sideEffects.StockTransactions[0].Type.Should().Be("SaleOut");
        sideEffects.StockTransactions[0].Qty.Should().Be(3);
        sideEffects.StockTransactions[0].OrderId.Should().Be(_orderId);

        // Verify repository interactions
        await _stockEntryRepository.Received(1).DeductStockWithBatchesAsync(item.SkuId, _storeId, 3, Arg.Any<CancellationToken>());
        await _stockTransactionRepository.Received(1).AddAsync(
            Arg.Is<StockTransaction>(tx => tx.Type == StockTransactionType.SaleOut && tx.Qty == 3 && tx.OrderId == _orderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStockFallsBelowMinStock_ShouldTriggerStockAlert()
    {
        // Arrange: Initial stock 5, order buys 4, minStock is 2 -> remaining 1 <= 2 -> triggers alert
        var order = CreatePaidOrder(qty: 4);
        var item = order.Items.First();
        var stockEntry = new StockEntry(_storeId, item.SkuId, qtyOnHand: 5, minStock: 2);

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>()).Returns(stockEntry);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StockAlerts.Should().HaveCount(1);
        var alert = result.Value.StockAlerts[0];
        alert.SkuId.Should().Be(item.SkuId);
        alert.AlertType.Should().Be("MinStock");
        alert.QtyOnHand.Should().Be(1); // 5 - 4 = 1
        alert.MinStock.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenStockRemainsAboveMinStock_ShouldNotTriggerStockAlert()
    {
        // Arrange: Initial stock 50, order buys 2, minStock is 5 -> remaining 48 > 5 -> no alert
        var order = CreatePaidOrder(qty: 2);
        var item = order.Items.First();
        var stockEntry = new StockEntry(_storeId, item.SkuId, qtyOnHand: 50, minStock: 5);

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>()).Returns(stockEntry);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StockAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenOrderHasActiveCustomer_ShouldAccruePointsAndRecordSpending()
    {
        // Arrange: GrandTotal = 200,000 VND, Normal tier rate = 0.01 (1%) -> 2,000 points
        var customerId = Guid.NewGuid();
        var order = CreatePaidOrder(customerId: customerId, itemPrice: 100_000m, qty: 2m);
        var item = order.Items.First();

        var customer = new Customer("Trần Thị B", "0912345678", _normalTier.Id, id: customerId);
        customer.SetMemberTier(_normalTier.Id);
        typeof(Customer).GetProperty(nameof(Customer.MemberTier))!.SetValue(customer, _normalTier);

        var loyaltyAccount = new LoyaltyAccount(customerId, pointsBalance: 500);

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>())
            .Returns(new StockEntry(_storeId, item.SkuId, 50, 5));
        _customerRepository.GetEntityByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.GetLoyaltyAccountWithTierAsync(customerId, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);
        _memberTierRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_normalTier, _silverTier, _goldTier]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointTransaction.Should().NotBeNull();
        result.Value.PointTransaction!.Points.Should().Be(2_000);
        result.Value.PointTransaction.Type.Should().Be("Earn");
        result.Value.PointTransaction.OrderId.Should().Be(_orderId);

        loyaltyAccount.PointsBalance.Should().Be(2_500); // 500 + 2,000
        customer.TotalSpending.Should().Be(200_000);

        await _customerRepository.Received(1).AddPointTransactionAsync(
            Arg.Is<PointTransaction>(pt => pt.CustomerId == customerId && pt.Points == 2_000 && pt.Type == PointTransactionType.Earn),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCustomerSpendingExceedsThreshold_ShouldUpgradeTier()
    {
        // Arrange: Customer has 900,000 spending (Normal tier), buys 200,000 -> Total 1,100,000 >= 1,000,000 (Silver tier)
        var customerId = Guid.NewGuid();
        var order = CreatePaidOrder(customerId: customerId, itemPrice: 200_000m, qty: 1m);
        var item = order.Items.First();

        var customer = new Customer("Lê Văn C", "0987654321", _normalTier.Id, id: customerId);
        customer.RecordSpending(900_000, [_normalTier, _silverTier, _goldTier]);

        var loyaltyAccount = new LoyaltyAccount(customerId, 100);

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>())
            .Returns(new StockEntry(_storeId, item.SkuId, 50, 5));
        _customerRepository.GetEntityByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.GetLoyaltyAccountWithTierAsync(customerId, Arg.Any<CancellationToken>()).Returns(loyaltyAccount);
        _memberTierRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_normalTier, _silverTier, _goldTier]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        customer.TotalSpending.Should().Be(1_100_000);
        customer.MemberTierId.Should().Be(_silverTier.Id); // Upgraded to Silver
    }

    [Fact]
    public async Task Handle_WhenOrderHasNoCustomer_ShouldNotTriggerLoyalty()
    {
        // Arrange: Walk-in customer (customerId = null)
        var order = CreatePaidOrder(customerId: null);
        var item = order.Items.First();

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>())
            .Returns(new StockEntry(_storeId, item.SkuId, 50, 5));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointTransaction.Should().BeNull();
        await _customerRepository.DidNotReceive().GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _customerRepository.DidNotReceive().AddPointTransactionAsync(Arg.Any<PointTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCustomerIsInactive_ShouldNotAccruePoints()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var order = CreatePaidOrder(customerId: customerId);
        var item = order.Items.First();

        var customer = new Customer("Khách Bị Khóa", "0911222333", _normalTier.Id, id: customerId);
        customer.Deactivate();

        _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
        _stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, _storeId, Arg.Any<CancellationToken>())
            .Returns(new StockEntry(_storeId, item.SkuId, 50, 5));
        _customerRepository.GetEntityByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ProcessOrderPaidCommand(_orderId, _employeeId, order), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PointTransaction.Should().BeNull();
        await _customerRepository.DidNotReceive().AddPointTransactionAsync(Arg.Any<PointTransaction>(), Arg.Any<CancellationToken>());
    }
}
