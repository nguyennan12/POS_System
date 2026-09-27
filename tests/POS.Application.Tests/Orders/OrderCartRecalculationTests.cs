using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.AddOrderItem;
using POS.Application.UseCases.Orders.Commands.ApplyVoucher;
using POS.Application.UseCases.Orders.Commands.CreateOrder;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Services;
using POS.Domain.Customers;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Services;
using POS.Domain.Promotions.Services.Models;
using POS.Domain.Stores;

namespace POS.Application.Tests.Orders;

public class OrderCartRecalculationTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ISkuRepository _skuRepository = Substitute.For<ISkuRepository>();
    private readonly IShiftRepository _shiftRepository = Substitute.For<IShiftRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeStoreAccessRepository _employeeStoreAccessRepository = Substitute.For<IEmployeeStoreAccessRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IVoucherRepository _voucherRepository = Substitute.For<IVoucherRepository>();
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPromotionEngine _promotionEngine = new PromotionEngine();

    private readonly ICartCalculationService _cartCalculationService;

    public OrderCartRecalculationTests()
    {
        _cartCalculationService = new CartCalculationService(
            _skuRepository,
            _promotionRepository,
            _voucherRepository,
            _customerRepository,
            _promotionEngine);
    }

    [Fact]
    public async Task CreateOrder_ShouldFail_WhenShiftIsClosed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            name: "Cashier A",
            username: "cashier1",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);

        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var closedShift = Shift.Open(storeId, employeeId, 500_000, null);
        closedShift.Close(500_000, 500_000, null);

        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(closedShift);

        var handler = new CreateOrderCommandHandler(
            _orderRepository,
            _shiftRepository,
            _employeeRepository,
            _employeeStoreAccessRepository,
            _customerRepository,
            _unitOfWork,
            _currentUser);

        var command = new CreateOrderCommand(shiftId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.ShiftClosed.Code);
    }

    [Fact]
    public async Task CreateOrder_ShouldSucceed_WhenShiftIsOpen()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            name: "Cashier A",
            username: "cashier1",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);

        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var openShift = Shift.Open(storeId, employeeId, 500_000, null);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(openShift);

        _orderRepository.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.Arg<Guid>();
                return Order.CreateDraft(storeId, openShift.Id, employeeId, null, "VND", null, id);
            });

        var handler = new CreateOrderCommandHandler(
            _orderRepository,
            _shiftRepository,
            _employeeRepository,
            _employeeStoreAccessRepository,
            _customerRepository,
            _unitOfWork,
            _currentUser);

        var command = new CreateOrderCommand(shiftId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StoreId.Should().Be(storeId);
        result.Value.ShiftId.Should().Be(openShift.Id);
        result.Value.Status.Should().Be(OrderStatus.Draft.ToString());
        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderItem_ShouldRecalculatePromotionsAndTax_Instantly()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            name: "Cashier A",
            username: "cashier1",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);
        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var openShift = Shift.Open(storeId, employeeId, 500_000, null);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(openShift);

        var product = new Product(storeId, categoryId, "Cà phê sữa đá", "Ly");
        var sku = new Sku(
            productId: product.Id,
            storeId: storeId,
            skuCode: "CF-SUA",
            barcode: "8930001",
            sellPrice: 30_000,
            costPrice: 10_000,
            taxRate: 10, // 10% VAT
            isActive: true);
        sku.Product = product;

        var order = Order.CreateDraft(storeId, openShift.Id, employeeId, null, "VND", null, orderId);

        _orderRepository.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _skuRepository.GetByIdWithProductAsync(sku.Id, Arg.Any<CancellationToken>()).Returns(sku);
        _skuRepository.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([sku]);

        // Tạo khuyến mãi giảm 20% cho SKU này
        var promo = new Promotion(
            storeId: storeId,
            name: "Giảm 20% Cà phê",
            type: PromotionType.PercentSku,
            value: 20, // 20%
            appliesTo: PromotionAppliesTo.SKU,
            validFrom: DateTime.UtcNow.AddDays(-1),
            validTo: DateTime.UtcNow.AddDays(1),
            status: PromotionStatus.Active);
        promo.AddTargetSku(sku.Id);

        _promotionRepository.GetActiveAutomaticPromotionsAsync(storeId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([promo]);

        var handler = new AddOrderItemCommandHandler(
            _orderRepository,
            _skuRepository,
            _shiftRepository,
            _employeeRepository,
            _employeeStoreAccessRepository,
            _voucherRepository,
            _cartCalculationService,
            _unitOfWork,
            _currentUser);

        // Act: Thêm 2 ly cà phê (30,000 * 2 = 60,000)
        var command = new AddOrderItemCommand(orderId, sku.Id, 2);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert:
        // Subtotal = 60,000
        // Discount 20% = 12,000
        // Net = 48,000
        // Tax (10%) = 4,800
        // GrandTotal = 48,000 + 4,800 = 52,800
        result.IsSuccess.Should().BeTrue();
        result.Value!.Subtotal.Should().Be(60_000);
        result.Value.DiscountTotal.Should().Be(12_000);
        result.Value.TaxTotal.Should().Be(4_800);
        result.Value.GrandTotal.Should().Be(52_800);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].DiscountAmount.Should().Be(12_000);
        result.Value.Items[0].TaxAmount.Should().Be(4_800);
        result.Value.Items[0].LineTotal.Should().Be(52_800);
    }

    [Fact]
    public async Task AddOrderItem_ShouldSumQuantity_WhenAddingSameSkuMultipleTimes()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            name: "Cashier A",
            username: "cashier1",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);
        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var openShift = Shift.Open(storeId, employeeId, 500_000, null);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(openShift);

        var product = new Product(storeId, Guid.NewGuid(), "Bánh mì Pate", "Ổ");
        var sku = new Sku(
            productId: product.Id,
            storeId: storeId,
            skuCode: "BM-PATE",
            barcode: "8930002",
            sellPrice: 20_000,
            costPrice: 8_000,
            taxRate: 8, // 8% VAT
            isActive: true);
        sku.Product = product;

        var order = Order.CreateDraft(storeId, openShift.Id, employeeId, null, "VND", null, orderId);

        _orderRepository.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _skuRepository.GetByIdWithProductAsync(sku.Id, Arg.Any<CancellationToken>()).Returns(sku);
        _skuRepository.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([sku]);
        _promotionRepository.GetActiveAutomaticPromotionsAsync(storeId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new AddOrderItemCommandHandler(
            _orderRepository,
            _skuRepository,
            _shiftRepository,
            _employeeRepository,
            _employeeStoreAccessRepository,
            _voucherRepository,
            _cartCalculationService,
            _unitOfWork,
            _currentUser);

        // Act: Quét lần 1 (1 cái) và Quét lần 2 (2 cái)
        await handler.Handle(new AddOrderItemCommand(orderId, sku.Id, 1), CancellationToken.None);
        var result = await handler.Handle(new AddOrderItemCommand(orderId, sku.Id, 2), CancellationToken.None);

        // Assert: Tổng Qty = 3.
        // Subtotal = 20,000 * 3 = 60,000.
        // Discount = 0.
        // Tax = 60,000 * 8% = 4,800.
        // GrandTotal = 64,800.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Qty.Should().Be(3);
        result.Value.Subtotal.Should().Be(60_000);
        result.Value.DiscountTotal.Should().Be(0);
        result.Value.TaxTotal.Should().Be(4_800);
        result.Value.GrandTotal.Should().Be(64_800);
    }

    [Fact]
    public async Task ApplyVoucher_ShouldFail_WhenCartIsEmpty()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            name: "Cashier A",
            username: "cashier1",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);
        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var openShift = Shift.Open(storeId, employeeId, 500_000, null);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(openShift);

        // Giỏ hàng rỗng (0 items)
        var order = Order.CreateDraft(storeId, openShift.Id, employeeId, null, "VND", null, orderId);
        _orderRepository.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ApplyVoucherCommandHandler(
            _orderRepository,
            _shiftRepository,
            _employeeRepository,
            _employeeStoreAccessRepository,
            _voucherRepository,
            _cartCalculationService,
            _unitOfWork,
            _currentUser);

        // Act
        var result = await handler.Handle(new ApplyVoucherCommand(orderId, "SALE30K"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.CartEmpty.Code);
    }

    [Fact]
    public async Task ApplyVoucher_ShouldRecalculateCart_WhenVoucherIsValid()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            name: "Cashier A",
            username: "cashier1",
            passwordHash: "hash",
            pinHash: "pinhash",
            roleId: Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);
        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var openShift = Shift.Open(storeId, employeeId, 500_000, null);
        _shiftRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(openShift);

        var product = new Product(storeId, Guid.NewGuid(), "Trà sữa Trân châu", "Ly");
        var sku = new Sku(
            productId: product.Id,
            storeId: storeId,
            skuCode: "TS-TC",
            barcode: "8930003",
            sellPrice: 50_000,
            costPrice: 15_000,
            taxRate: 10,
            isActive: true);
        sku.Product = product;

        var order = Order.CreateDraft(storeId, openShift.Id, employeeId, null, "VND", null, orderId);
        order.AddOrUpdateItem(sku, 2); // 100,000 VND

        var voucherPromo = new Promotion(
            storeId: storeId,
            name: "Voucher giảm 30k toàn đơn",
            type: PromotionType.CartFixed,
            value: 30_000,
            minOrderAmount: 50_000,
            validFrom: DateTime.UtcNow.AddDays(-1),
            validTo: DateTime.UtcNow.AddDays(5),
            status: PromotionStatus.Active);

        var voucher = new Voucher(
            promotionId: voucherPromo.Id,
            code: "SALE30K",
            maxUses: 100,
            perCustomerLimit: 1,
            expiresAt: DateTime.UtcNow.AddDays(5),
            isActive: true);

        // Voucher gắn promotion
        typeof(Voucher).GetProperty("Promotion")?.SetValue(voucher, voucherPromo);

        _orderRepository.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _skuRepository.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([sku]);
        _voucherRepository.GetByCodeWithPromotionAsync("SALE30K", Arg.Any<CancellationToken>()).Returns(voucher);
        _voucherRepository.GetByIdWithPromotionAsync(voucher.Id, Arg.Any<CancellationToken>()).Returns(voucher);
        _promotionRepository.GetActiveAutomaticPromotionsAsync(storeId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new ApplyVoucherCommandHandler(
            _orderRepository,
            _shiftRepository,
            _employeeRepository,
            _employeeStoreAccessRepository,
            _voucherRepository,
            _cartCalculationService,
            _unitOfWork,
            _currentUser);

        // Act
        var result = await handler.Handle(new ApplyVoucherCommand(orderId, "SALE30K"), CancellationToken.None);

        // Assert:
        // Subtotal = 100,000
        // Voucher Discount = 30,000
        // Net = 70,000
        // Tax (10%) = 7,000
        // GrandTotal = 77,000
        result.IsSuccess.Should().BeTrue();
        result.Value!.Subtotal.Should().Be(100_000);
        result.Value.DiscountTotal.Should().Be(30_000);
        result.Value.TaxTotal.Should().Be(7_000);
        result.Value.GrandTotal.Should().Be(77_000);
        result.Value.Discounts.Should().HaveCount(1);
        result.Value.Discounts[0].DiscountAmount.Should().Be(30_000);
    }

    [Fact]
    public void PromotionEngine_CartPromotionWithTargetSku_ShouldOnlyDiscountTargetItems()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var skuTargetId = Guid.NewGuid();
        var skuOtherId = Guid.NewGuid();

        var promo = new Promotion(
            storeId: storeId,
            name: "Giảm 10% cho SKU Target",
            type: PromotionType.CartPercent,
            value: 10,
            appliesTo: PromotionAppliesTo.SKU,
            status: PromotionStatus.Active);
        promo.AddTargetSku(skuTargetId);

        var cart = new PromotionCart(
            StoreId: storeId,
            Items:
            [
                new PromotionCartItem(skuTargetId, "SKU-TARGET", Guid.NewGuid(), 1, 100_000), // 100,000
                new PromotionCartItem(skuOtherId, "SKU-OTHER", Guid.NewGuid(), 1, 200_000)   // 200,000
            ]
        );

        // Act
        var result = _promotionEngine.Evaluate(cart, [promo], DateTime.UtcNow);

        // Assert:
        // Subtotal = 300,000
        // Chiết khấu 10% chỉ trên SKU-TARGET (100,000 * 10% = 10,000)
        // SKU-OTHER nhận 0 VND chiết khấu!
        result.Subtotal.Should().Be(300_000);
        result.TotalDiscount.Should().Be(10_000);
        result.GrandTotal.Should().Be(290_000);

        var targetDiscount = result.ItemDiscounts.First(i => i.SkuId == skuTargetId);
        targetDiscount.DiscountAmount.Should().Be(10_000);
        targetDiscount.FinalLineTotal.Should().Be(90_000);

        var otherDiscount = result.ItemDiscounts.First(i => i.SkuId == skuOtherId);
        otherDiscount.DiscountAmount.Should().Be(0);
        otherDiscount.FinalLineTotal.Should().Be(200_000);
    }

    [Fact]
    public async Task CartCalculationService_ShouldReturnSkuNotFound_WhenSkuNotFoundInRepository()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var nonExistentSkuId = Guid.NewGuid();

        var dummySku = new Sku(Guid.NewGuid(), storeId, "DUMMY", "0000", 10_000, 5_000, 10, true, null, nonExistentSkuId);
        var order = Order.CreateDraft(storeId, shiftId, employeeId, null, "VND", null, orderId);
        order.AddOrUpdateItem(dummySku, 1);

        // Mock DB không tìm thấy SKU này
        _skuRepository.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _cartCalculationService.RecalculateAsync(order);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OrderErrors.SkuNotFound.Code);
    }
}
