using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.CancelOrder;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.Commands.ProcessOrderPaid;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Services;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Payments;
using POS.Application.UseCases.Payments.Strategies;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;

namespace POS.Application.Tests.Orders;


public partial class CheckoutAndCancelOrderTests
{
  private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
  private readonly IShiftRepository _shiftRepository = Substitute.For<IShiftRepository>();
  private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
  private readonly IStoreRepository _storeRepository = Substitute.For<IStoreRepository>();
  private readonly IStockEntryRepository _stockEntryRepository = Substitute.For<IStockEntryRepository>();
  private readonly IStockTransactionRepository _stockTransactionRepository = Substitute.For<IStockTransactionRepository>();
  private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
  private readonly IVoucherRepository _voucherRepository = Substitute.For<IVoucherRepository>();
  private readonly IVoucherUsageRepository _voucherUsageRepository = Substitute.For<IVoucherUsageRepository>();
  private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
  private readonly IMemberTierRepository _memberTierRepository = Substitute.For<IMemberTierRepository>();
  private readonly IPaymentStrategyFactory _paymentStrategyFactory;
  private readonly ICartCalculationService _cartCalculationService = Substitute.For<ICartCalculationService>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
  private readonly IPublisher _publisher = Substitute.For<IPublisher>();
  private readonly ICheckoutGuardService _checkoutGuardService = Substitute.For<ICheckoutGuardService>();

  private readonly Guid _storeId = Guid.NewGuid();
  private readonly Guid _shiftId;
  private readonly Guid _employeeId = Guid.NewGuid();
  private readonly Guid _orderId = Guid.NewGuid();

  private readonly Employee _cashierEmployee;
  private readonly Employee _managerEmployee;
  private readonly Shift _openShift;
  private readonly Store _store;

  public CheckoutAndCancelOrderTests()
  {
    _store = new Store(
        name: "Cửa hàng 1",
        address: "123 Đường ABC",
        phone: "0123456789",
        timezone: "Asia/Ho_Chi_Minh",
        currencyCode: "VND",
        taxCode: "store1",
        receiptHeader: "Header",
        receiptFooter: "Footer",
        isActive: true,
        id: _storeId);

    var cashierRole = new Role(RoleNames.Cashier, isSystemRole: true);
    var managerRole = new Role(RoleNames.StoreManager, isSystemRole: true);

    _cashierEmployee = new Employee(
        name: "Thu ngân A",
        username: "cashier1",
        passwordHash: "hash",
        pinHash: "pin",
        roleId: cashierRole.Id,
        isChainOwner: false,
        storeId: _storeId,
        isActive: true,
        id: _employeeId);
    typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(_cashierEmployee, cashierRole);

    _managerEmployee = new Employee(
        name: "Quản lý B",
        username: "manager1",
        passwordHash: "hash",
        pinHash: "pin",
        roleId: managerRole.Id,
        isChainOwner: false,
        storeId: _storeId,
        isActive: true,
        id: Guid.NewGuid());
    typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(_managerEmployee, managerRole);

    _openShift = Shift.Open(_storeId, _employeeId, 500_000, null);
    _shiftId = _openShift.Id;

    // Default: guard service returns employee + shift/store success, and inventory passes
    _checkoutGuardService.ValidateEmployeeAuthAsync(Arg.Any<CancellationToken>())
        .Returns(Result<Employee>.Success(_cashierEmployee));
    _checkoutGuardService.ValidatePreCheckoutGuardsAsync(Arg.Any<Order>(), Arg.Any<Employee>(), Arg.Any<CancellationToken>())
        .Returns(Result<(Shift, Store)>.Success((_openShift, _store)));
    _checkoutGuardService.ValidateInventoryAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
        .Returns(Result.Success());

    _storeRepository.GetByIdAsync(_storeId, Arg.Any<CancellationToken>()).Returns(_store);
    _stockEntryRepository.GetBySkuAndStoreAsync(Arg.Any<Guid>(), _storeId, Arg.Any<CancellationToken>())
        .Returns(ci => new StockEntry(_storeId, ci.ArgAt<Guid>(0), 1000));

    _cartCalculationService.RecalculateAsync(Arg.Any<Order>(), Arg.Any<Voucher>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
        .Returns(Result.Success());

    _invoiceRepository.ExistsForOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
    _invoiceRepository.GetNextSequenceAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(1);
    _memberTierRepository.GetAllAsync(Arg.Any<CancellationToken>())
        .Returns([new MemberTier(POS.Domain.Customers.Enums.MemberTierName.Normal, 0m, 0.01m, 0m, "#808080")]);

    var strategies = new IPaymentStrategy[]
    {
        new CashPaymentStrategy(),
        new MoMoPaymentStrategy(),
        new VietQrPaymentStrategy(),
        new CardPaymentStrategy(),
        new PointsPaymentStrategy(_customerRepository)
    };
    _paymentStrategyFactory = new PaymentStrategyFactory(strategies);

    // Mock unitOfWork.ExecuteSerializableAsync to invoke the delegate directly
    _unitOfWork.ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<Result<CheckoutDto>>>>(), Arg.Any<CancellationToken>())
        .Returns(ci => ci.Arg<Func<CancellationToken, Task<Result<CheckoutDto>>>>()(ci.Arg<CancellationToken>()));
  }

  private CheckoutOrderCommandHandler CreateCheckoutHandler(ICartCalculationService? calculationService = null) => new(
      _orderRepository,
      _checkoutGuardService,
      _paymentStrategyFactory,
      calculationService ?? _cartCalculationService,
      _customerRepository,
      _publisher,
      _unitOfWork);

  private Order CreateDraftOrderWithItems(decimal itemPrice = 100_000, decimal qty = 1)
  {
    var order = Order.CreateDraft(_storeId, _shiftId, _employeeId, null, "VND", null, _orderId);
    typeof(Order).GetProperty(nameof(Order.Store))!.SetValue(order, _store);

    var product = new Product(_storeId, Guid.NewGuid(), "Sản phẩm A", "Cái");
    var sku = new Sku(product.Id, _storeId, "SKU-A", "123456", itemPrice, itemPrice / 2, 0, true);
    sku.Product = product;

    order.AddOrUpdateItem(sku, qty);

    var subtotal = itemPrice * qty;
    var promoResult = new PromotionResult(
        Subtotal: subtotal,
        GrandTotal: subtotal,
        TotalDiscount: 0,
        ItemDiscounts: [],
        OrderDiscounts: [],
        AppliedPromotions: []
    );
    order.ApplyPromotionEvaluation(promoResult, new Dictionary<Guid, decimal>());
    return order;
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Checkout_ShouldComplete_WithRealCalculationService(bool split)
  {
    var order = CreateDraftOrderWithItems();
    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
    var skus = Substitute.For<ISkuRepository>();
    skus.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
        .Returns(order.Items.Select(i => i.Sku).ToList());
    var promotions = Substitute.For<IPromotionRepository>();
    promotions.GetActiveAutomaticPromotionsAsync(_storeId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
        .Returns(new List<Promotion>());
    var service = new CartCalculationService(skus, promotions, _voucherRepository,
        _customerRepository, new POS.Domain.Promotions.Services.PromotionEngine());

    var handler = CreateCheckoutHandler(service);
    if (split)
    {
      var partial = await handler.Handle(new(_orderId, [new("Cash", 40_000)]), CancellationToken.None);
      partial.IsSuccess.Should().BeTrue();
      order.Status.Should().Be(OrderStatus.Confirmed);
    }
    var result = await handler.Handle(
        new(_orderId, [new("Cash", split ? 60_000 : 100_000)]), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    order.Status.Should().Be(OrderStatus.Paid);
  }

  [Fact]
  public async Task Checkout_ShouldTransitionToPaid_WhenPaymentAmountMatchesGrandTotal()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.GrandTotal.Should().Be(100_000);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Cash", 100_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value!.Status.Should().Be("Paid");
    result.Value.TotalPaid.Should().Be(100_000);
    result.Value.ChangeAmount.Should().Be(0);
    result.Value.Payments.Should().HaveCount(1);
    result.Value.ReceiptData.Should().NotBeNull();
    result.Value.ReceiptData!.GrandTotal.Should().Be(100_000);
    result.Value.ReceiptData.AmountPaid.Should().Be(100_000);
    result.Value.ReceiptData.CashierName.Should().Be(_cashierEmployee.Name);

    order.Status.Should().Be(OrderStatus.Paid);
    order.PaidAt.Should().NotBeNull();

    // Side effects (inventory deduction, invoice) are delegated to domain event handlers
    // via publisher.Publish(OrderPaidDomainEvent) — verify the event was published
    await _publisher.Received(1).Publish(
        Arg.Is<OrderPaidDomainEvent>(e => e.OrderId == _orderId),
        Arg.Any<CancellationToken>());
  }


  [Fact]
  public async Task Checkout_ShouldReplayWholeDecisionAfterSerializableConflict()
  {
    var order = CreateDraftOrderWithItems(itemPrice: 100_000);
    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
    var attempts = 0;
    _unitOfWork.ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<Result<CheckoutDto>>>>(),
        Arg.Any<CancellationToken>()).Returns(async ci =>
    {
      attempts++;
      if (attempts == 1)
        return Result<CheckoutDto>.Failure(new Error(ErrorType.Invalid, "Persistence.ConcurrentModification"));
      return await ci.Arg<Func<CancellationToken, Task<Result<CheckoutDto>>>>()(ci.Arg<CancellationToken>());
    });

    var result = await CreateCheckoutHandler().Handle(
        new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]), default);

    result.IsSuccess.Should().BeTrue();
    attempts.Should().Be(2);
    // Event is published (handlers deduct stock, generate invoice)
    await _publisher.Received().Publish(
        Arg.Is<OrderPaidDomainEvent>(e => e.OrderId == _orderId),
        Arg.Any<CancellationToken>());
  }


  [Fact]
  public async Task Checkout_ShouldTransitionToPaid_AndCalculateChangeAmount_WhenCashOverpays()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 120_000, qty: 1);
    order.GrandTotal.Should().Be(120_000);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    // Khách đưa 150,000 VND tiền mặt
    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Cash", 150_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value!.Status.Should().Be("Paid");
    result.Value.TotalPaid.Should().Be(120_000);
    result.Value.ChangeAmount.Should().Be(30_000);
    result.Value.Payments.Should().HaveCount(1);
    result.Value.Payments[0].ChangeAmount.Should().Be(30_000);
    result.Value.ReceiptData.Should().NotBeNull();
    result.Value.ReceiptData!.ChangeAmount.Should().Be(30_000);

    order.Status.Should().Be(OrderStatus.Paid);
    order.PaidAt.Should().NotBeNull();
  }

  [Fact]
  public async Task Checkout_ShouldRemainConfirmed_WhenTotalPaidIsLessThanGrandTotal()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 200_000, qty: 1);
    order.GrandTotal.Should().Be(200_000);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    // Khách trả trước 80,000 VND
    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Cash", 80_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value!.Status.Should().Be("Confirmed");
    result.Value.TotalPaid.Should().Be(80_000);
    result.Value.ChangeAmount.Should().Be(0);
    result.Value.ReceiptData.Should().BeNull(); // Chưa in bill hoàn tất

    order.Status.Should().Be(OrderStatus.Confirmed);
    order.PaidAt.Should().BeNull();
  }

  [Fact]
  public async Task Checkout_ShouldTransitionToPaid_OnSubsequentPayment_WhenCumulativeTotalMatchesGrandTotal()
  {
    // Arrange: Đơn hàng 200,000 đã confirm và trả trước 80,000
    var order = CreateDraftOrderWithItems(itemPrice: 200_000, qty: 1);
    order.Confirm();
    order.ProcessPayments([(PaymentMethod.Cash, 80_000, null)]);
    order.Status.Should().Be(OrderStatus.Confirmed);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    // Khách thanh toán phần còn lại: 120,000 qua VietQR
    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("VietQR", 120_000, "VQR-TX123")]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value!.Status.Should().Be("Paid");
    result.Value.TotalPaid.Should().Be(200_000);
    result.Value.Payments.Should().HaveCount(2);
    result.Value.ReceiptData.Should().NotBeNull();
    result.Value.ReceiptData!.GrandTotal.Should().Be(200_000);
    result.Value.ReceiptData.AmountPaid.Should().Be(200_000);

    order.Status.Should().Be(OrderStatus.Paid);
    order.PaidAt.Should().NotBeNull();
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenOrderIsAlreadyPaid()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.Confirm();
    order.ProcessPayments([(PaymentMethod.Cash, 100_000, null)]);
    order.Status.Should().Be(OrderStatus.Paid);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Cash", 10_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.AlreadyPaid.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenNonCashOverpays()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.GrandTotal.Should().Be(100_000);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    // MoMo vượt grand total
    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("MoMo", 150_000, "MM-123")]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.NonCashOverpaymentNotAllowed.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenElectronicPaymentLacksTransactionRef()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Card", 100_000, null)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.TransactionRefRequired.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenStockIsInsufficient()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 5);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    // Guard service returns stock insufficient error (tồn kho chỉ có 2, yêu cầu 5)
    _checkoutGuardService.ValidateInventoryAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
        .Returns(Result.Failure(OrderErrors.StockInsufficient));

    var handler = CreateCheckoutHandler();
    var command = new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 500_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.StockInsufficient.Code);
  }


  [Fact]
  public async Task Checkout_ShouldSucceed_WhenShiftOpenedByManagerInSameStore()
  {
    // Arrange: Ca do Quản lý mở, Thu ngân bán hàng -> Thành công
    // Default guard service mock already returns success — this test verifies the success path
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();
    var command = new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    order.Status.Should().Be(OrderStatus.Paid);
  }


  [Fact]
  public async Task Checkout_ShouldFail_WhenShiftBelongsToAnotherCashierInSameStore()
  {
    // Arrange: Ca do Thu ngân khác mở → guard service trả ShiftNotOwned
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
    _checkoutGuardService.ValidatePreCheckoutGuardsAsync(Arg.Any<Order>(), Arg.Any<Employee>(), Arg.Any<CancellationToken>())
        .Returns(Result<(Shift, Store)>.Failure(OrderErrors.ShiftNotOwned));

    var handler = CreateCheckoutHandler();
    var result = await handler.Handle(
        new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.ShiftNotOwned.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenShiftStoreMismatch()
  {
    // Arrange: Shift storeId ≠ order storeId → guard service trả ShiftStoreMismatch
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
    _checkoutGuardService.ValidatePreCheckoutGuardsAsync(Arg.Any<Order>(), Arg.Any<Employee>(), Arg.Any<CancellationToken>())
        .Returns(Result<(Shift, Store)>.Failure(OrderErrors.ShiftStoreMismatch));

    var handler = CreateCheckoutHandler();
    var result = await handler.Handle(
        new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.ShiftStoreMismatch.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenStoreIsInactive()
  {
    // Arrange: Store inactive → guard service trả StoreInactive
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
    _checkoutGuardService.ValidatePreCheckoutGuardsAsync(Arg.Any<Order>(), Arg.Any<Employee>(), Arg.Any<CancellationToken>())
        .Returns(Result<(Shift, Store)>.Failure(OrderErrors.StoreInactive));

    var handler = CreateCheckoutHandler();
    var result = await handler.Handle(
        new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.StoreInactive.Code);
  }


  [Fact]
  public async Task Checkout_ShouldFail_WhenPointsUsedWithoutCustomer()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.CustomerId.Should().BeNull();

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Points", 50_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.PointsRequireCustomer.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenCustomerHasInsufficientPoints()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    var customerId = Guid.NewGuid();
    order.SetCustomer(customerId);

    var customer = new Customer("Khách A", "0987654321", Guid.NewGuid(), null, null, null, true, customerId);
    _customerRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>())
        .Returns(new CustomerWithPoints(customer, PointsBalance: 20_000));

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();

    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Points", 50_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.InsufficientPoints.Code);
  }

  [Fact]
  public async Task CancelOrder_ShouldSucceed_WhenRequestedByStoreManager()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);

    _currentUser.EmployeeId.Returns(_managerEmployee.Id);
    _employeeRepository.GetByIdAsync(_managerEmployee.Id, Arg.Any<CancellationToken>()).Returns(_managerEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = new CancelOrderCommandHandler(
        _orderRepository,
        _employeeRepository,
        _unitOfWork,
        _currentUser);

    var command = new CancelOrderCommand(_orderId, "Khách đổi ý không mua nữa");

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value!.Status.Should().Be(OrderStatus.Cancelled.ToString());
    order.Status.Should().Be(OrderStatus.Cancelled);
    order.CancelReason.Should().Be("Khách đổi ý không mua nữa");
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task CancelOrder_ShouldFail_WhenOrderIsAlreadyPaid()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.Confirm();
    order.ProcessPayments([(PaymentMethod.Cash, 100_000, null)]);
    order.Status.Should().Be(OrderStatus.Paid);

    _currentUser.EmployeeId.Returns(_managerEmployee.Id);
    _employeeRepository.GetByIdAsync(_managerEmployee.Id, Arg.Any<CancellationToken>()).Returns(_managerEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = new CancelOrderCommandHandler(
        _orderRepository,
        _employeeRepository,
        _unitOfWork,
        _currentUser);

    var command = new CancelOrderCommand(_orderId, "Hủy đơn đã thanh toán");

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.CannotCancelPaidOrder.Code);
    order.Status.Should().Be(OrderStatus.Paid);
  }

  [Fact]
  public async Task CancelOrder_ShouldFail_WhenOrderHasSuccessfulPayments()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.Confirm();
    order.ProcessPayments([(PaymentMethod.Cash, 50_000, null)]);
    order.Status.Should().Be(OrderStatus.Confirmed);

    _currentUser.EmployeeId.Returns(_managerEmployee.Id);
    _employeeRepository.GetByIdAsync(_managerEmployee.Id, Arg.Any<CancellationToken>()).Returns(_managerEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = new CancelOrderCommandHandler(
        _orderRepository,
        _employeeRepository,
        _unitOfWork,
        _currentUser);

    var command = new CancelOrderCommand(_orderId, "Hủy đơn có thanh toán một phần");

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.CannotCancelOrderWithPayments.Code);
    order.Status.Should().Be(OrderStatus.Confirmed);
  }

  [Fact]
  public async Task CheckoutOrder_ShouldPublishDomainEvent_WhenOrderPaid_WithVoucher()
  {
    // Arrange: voucher recording is now handled by OrderPaidVoucherHandler via domain event
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    var promotion = new Promotion(_storeId, "Promo", PromotionType.CartFixed, 20_000, validFrom: DateTime.UtcNow.AddDays(-1), createdBy: _cashierEmployee.Id);
    var voucher = new Voucher(promotion.Id, "GUEST20K", maxUses: 10, expiresAt: DateTime.UtcNow.AddDays(1));
    typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, promotion);

    order.SetAppliedVoucher(voucher.Id, voucher.Code);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();
    var command = new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert: handler publishes domain event; the voucher handler runs independently
    result.IsSuccess.Should().BeTrue();
    await _publisher.Received(1).Publish(
        Arg.Is<OrderPaidDomainEvent>(e => e.OrderId == _orderId),
        Arg.Any<CancellationToken>());
  }


  [Fact]
  public async Task CheckoutOrder_ShouldSetCustomerOnDraft_WhenProvidedInCommand()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    var newCustomerId = Guid.NewGuid();
    var customer = new Customer("Khách Mới", "0933445566", Guid.NewGuid(), isActive: true, id: newCustomerId);
    _customerRepository.GetByIdAsync(newCustomerId, Arg.Any<CancellationToken>())
        .Returns(new CustomerWithPoints(customer, PointsBalance: 0));
    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();
    var command = new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)], newCustomerId);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    order.CustomerId.Should().Be(newCustomerId);
  }

  [Fact]
  public async Task CheckoutOrder_ShouldFail_WhenCustomerIsInactive()
  {
    // Arrange: Guard service trả CustomerErrors.Inactive khi khách hàng bị vô hiệu hóa
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    order.SetCustomer(Guid.NewGuid());
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);
    _checkoutGuardService.ValidatePreCheckoutGuardsAsync(Arg.Any<Order>(), Arg.Any<Employee>(), Arg.Any<CancellationToken>())
        .Returns(Result<(Shift, Store)>.Failure(POS.Domain.Customers.Errors.CustomerErrors.Inactive));

    var handler = CreateCheckoutHandler();
    var command = new CheckoutOrderCommand(_orderId, [new PaymentSplitInputDto("Cash", 100_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(POS.Domain.Customers.Errors.CustomerErrors.Inactive.Code);
    order.Status.Should().Be(OrderStatus.Draft);
  }

  [Fact]
  public async Task CheckoutOrder_ShouldSucceed_WhenGrandTotalIsZeroAndPaymentsListIsEmpty()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 0, qty: 1);
    order.GrandTotal.Should().Be(0);

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    var handler = CreateCheckoutHandler();
    var command = new CheckoutOrderCommand(_orderId, []);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value!.Status.Should().Be("Paid");
    result.Value.TotalPaid.Should().Be(0);
    result.Value.Payments.Should().BeEmpty();
    order.Status.Should().Be(OrderStatus.Paid);
  }
}
