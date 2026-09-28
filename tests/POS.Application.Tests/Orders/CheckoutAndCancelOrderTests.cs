using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.CancelOrder;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
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
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;

namespace POS.Application.Tests.Orders;

public class CheckoutAndCancelOrderTests
{
  private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
  private readonly IShiftRepository _shiftRepository = Substitute.For<IShiftRepository>();
  private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
  private readonly IEmployeeStoreAccessRepository _employeeStoreAccessRepository = Substitute.For<IEmployeeStoreAccessRepository>();
  private readonly IStoreRepository _storeRepository = Substitute.For<IStoreRepository>();
  private readonly IStockEntryRepository _stockEntryRepository = Substitute.For<IStockEntryRepository>();
  private readonly IStockTransactionRepository _stockTransactionRepository = Substitute.For<IStockTransactionRepository>();
  private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
  private readonly IVoucherRepository _voucherRepository = Substitute.For<IVoucherRepository>();
  private readonly IVoucherUsageRepository _voucherUsageRepository = Substitute.For<IVoucherUsageRepository>();
  private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
  private readonly IPaymentStrategyFactory _paymentStrategyFactory;
  private readonly ICartCalculationService _cartCalculationService = Substitute.For<ICartCalculationService>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

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
    _shiftRepository.GetByIdAsync(_shiftId, Arg.Any<CancellationToken>()).Returns(_openShift);

    _storeRepository.GetByIdAsync(_storeId, Arg.Any<CancellationToken>()).Returns(_store);
    _stockEntryRepository.GetBySkuAndStoreAsync(Arg.Any<Guid>(), _storeId, Arg.Any<CancellationToken>())
        .Returns(ci => new StockEntry(_storeId, ci.ArgAt<Guid>(0), 1000));

    _cartCalculationService.RecalculateAsync(Arg.Any<Order>(), Arg.Any<Voucher>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
        .Returns(Result.Success());

    _invoiceRepository.ExistsForOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
    _invoiceRepository.GetNextSequenceAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(1);

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

  private CheckoutOrderCommandHandler CreateCheckoutHandler() => new(
      _orderRepository,
      _shiftRepository,
      _employeeRepository,
      _employeeStoreAccessRepository,
      _storeRepository,
      _stockEntryRepository,
      _stockTransactionRepository,
      _invoiceRepository,
      _voucherRepository,
      _voucherUsageRepository,
      _paymentStrategyFactory,
      _cartCalculationService,
      _unitOfWork,
      _currentUser);

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

    // Verify side effects
    await _stockEntryRepository.Received(1).DeductStockWithBatchesAsync(Arg.Any<Guid>(), _storeId, 1, Arg.Any<CancellationToken>());
    await _stockTransactionRepository.Received(1).AddAsync(Arg.Any<StockTransaction>(), Arg.Any<CancellationToken>());
    await _invoiceRepository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
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

    _currentUser.EmployeeId.Returns(_employeeId);
    _employeeRepository.GetByIdAsync(_employeeId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
    _orderRepository.GetByIdWithDetailsAsync(_orderId, Arg.Any<CancellationToken>()).Returns(order);

    // Tồn kho chỉ có 2 (yêu cầu 5)
    _stockEntryRepository.GetBySkuAndStoreAsync(Arg.Any<Guid>(), _storeId, Arg.Any<CancellationToken>())
        .Returns(ci => new StockEntry(_storeId, ci.ArgAt<Guid>(0), 2));

    var handler = CreateCheckoutHandler();

    var command = new CheckoutOrderCommand(
        _orderId,
        [new PaymentSplitInputDto("Cash", 500_000)]);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.StockInsufficient.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenShiftBelongsToAnotherCashier()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);

    var otherEmployeeId = Guid.NewGuid();
    var otherShift = Shift.Open(_storeId, otherEmployeeId, 500_000, null);
    _shiftRepository.GetByIdAsync(order.ShiftId, Arg.Any<CancellationToken>()).Returns(otherShift);

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
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.ShiftNotOwned.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenShiftStoreMismatch()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);

    var otherStoreId = Guid.NewGuid();
    var otherShift = Shift.Open(otherStoreId, _employeeId, 500_000, null);
    _shiftRepository.GetByIdAsync(order.ShiftId, Arg.Any<CancellationToken>()).Returns(otherShift);

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
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(OrderErrors.ShiftStoreMismatch.Code);
  }

  [Fact]
  public async Task Checkout_ShouldFail_WhenStoreIsInactive()
  {
    // Arrange
    var order = CreateDraftOrderWithItems(itemPrice: 100_000, qty: 1);
    var inactiveStore = new Store("Inactive Store", isActive: false, id: _storeId);
    _storeRepository.GetByIdAsync(_storeId, Arg.Any<CancellationToken>()).Returns(inactiveStore);

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
        _employeeStoreAccessRepository,
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
        _employeeStoreAccessRepository,
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
}
