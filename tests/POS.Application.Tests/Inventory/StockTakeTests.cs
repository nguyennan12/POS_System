using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Application.UseCases.Inventory.StockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.ApproveStockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.CreateStockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.SubmitStockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.UpdateStockTakeItems;
using POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakeById;
using POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakes;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Inventory.StockTake;
using POS.Domain.Products;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;
using Xunit;

namespace POS.Application.Tests.Inventory;

public class StockTakeTests
{
    private readonly IStockTakeRepository _stockTakeRepository = Substitute.For<IStockTakeRepository>();
    private readonly IStockEntryRepository _stockEntryRepository = Substitute.For<IStockEntryRepository>();
    private readonly IStockTransactionRepository _stockTransactionRepository = Substitute.For<IStockTransactionRepository>();
    private readonly ISkuRepository _skuRepository = Substitute.For<ISkuRepository>();
    private readonly IStoreRepository _storeRepository = Substitute.For<IStoreRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _cashierId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly Store _store;
    private readonly Employee _cashierEmployee;
    private readonly Employee _managerEmployee;

    public StockTakeTests()
    {
        _unitOfWork.ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<Result<StockTakeDetailDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var func = callInfo.Arg<Func<CancellationToken, Task<Result<StockTakeDetailDto>>>>();
                return func(CancellationToken.None);
            });

        _store = new Store("Cửa hàng 1", "ST01", "123 Đường A", "0900000001", isActive: true, id: _storeId);

        var cashierRole = new Role(RoleNames.Cashier, isSystemRole: true);
        var managerRole = new Role(RoleNames.StoreManager, isSystemRole: true);

        _cashierEmployee = new Employee("Thu ngân A", "cashier_a", "hash", "pin", cashierRole.Id, isChainOwner: false, storeId: _storeId, isActive: true, id: _cashierId);
        typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(_cashierEmployee, cashierRole);

        _managerEmployee = new Employee("Quản lý B", "manager_b", "hash", "pin", managerRole.Id, isChainOwner: false, storeId: _storeId, isActive: true, id: _managerId);
        typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(_managerEmployee, managerRole);

        _storeRepository.GetByIdAsync(_storeId, Arg.Any<CancellationToken>()).Returns(_store);
        _employeeRepository.GetByIdAsync(_cashierId, Arg.Any<CancellationToken>()).Returns(_cashierEmployee);
        _employeeRepository.GetByIdAsync(_managerId, Arg.Any<CancellationToken>()).Returns(_managerEmployee);
    }

    [Fact]
    public async Task CreateStockTake_ShouldSucceed_WhenUserAndStoreAreValid()
    {
        // Arrange
        _currentUser.EmployeeId.Returns(_cashierId);
        _currentUser.StoreId.Returns(_storeId);

        Domain.Inventory.StockTake.StockTake? captured = null;
        await _stockTakeRepository.AddAsync(Arg.Do<Domain.Inventory.StockTake.StockTake>(st => captured = st), Arg.Any<CancellationToken>());
        _stockTakeRepository.GetByIdWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => captured);

        var handler = new CreateStockTakeCommandHandler(_stockTakeRepository, _storeRepository, _employeeRepository, _unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(new CreateStockTakeCommand("Kiểm kê tháng 10"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StoreId.Should().Be(_storeId);
        result.Value.Status.Should().Be(StockTakeStatus.Draft.ToString());
        result.Value.Note.Should().Be("Kiểm kê tháng 10");
        result.Value.CreatedBy.Should().Be(_cashierId);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStockTakeItems_ShouldAutomaticallyComputeDiffQty()
    {
        // Arrange
        _currentUser.EmployeeId.Returns(_cashierId);
        _currentUser.StoreId.Returns(_storeId);

        var stockTake = Domain.Inventory.StockTake.StockTake.Create(_storeId, _cashierId, "Đang kiểm đếm");
        _stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, Arg.Any<CancellationToken>()).Returns(stockTake);

        var product = new Product(_storeId, Guid.NewGuid(), "Trà sữa trân châu", "Ly");
        var sku1 = new Sku(product.Id, _storeId, "SKU-TS-01", "893000001", sellPrice: 30000m, id: Guid.NewGuid());
        var sku2 = new Sku(product.Id, _storeId, "SKU-TS-02", "893000002", sellPrice: 35000m, id: Guid.NewGuid());
        sku1.Product = product;
        sku2.Product = product;

        _skuRepository.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([sku1, sku2]);

        // SKU1: tồn hệ thống 10, đếm thực tế 8 -> chênh lệch -2 (thất thoát)
        var entry1 = new StockEntry(_storeId, sku1.Id, 10m);
        _stockEntryRepository.GetBySkuAndStoreAsync(sku1.Id, _storeId, Arg.Any<CancellationToken>())
            .Returns(entry1);

        // SKU2: tồn hệ thống 5, đếm thực tế 12 -> chênh lệch +7 (dư)
        var entry2 = new StockEntry(_storeId, sku2.Id, 5m);
        _stockEntryRepository.GetBySkuAndStoreAsync(sku2.Id, _storeId, Arg.Any<CancellationToken>())
            .Returns(entry2);

        var handler = new UpdateStockTakeItemsCommandHandler(
            _stockTakeRepository, _stockEntryRepository, _skuRepository, _employeeRepository, _unitOfWork, _currentUser);

        var command = new UpdateStockTakeItemsCommand(
            stockTake.Id,
            [
                new StockTakeItemInput(sku1.Id, 8m, "Mất 2 ly"),
                new StockTakeItemInput(sku2.Id, 12m, "Dư 7 ly")
            ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);

        var item1 = result.Value.Items.First(i => i.SkuId == sku1.Id);
        item1.SystemQty.Should().Be(10m);
        item1.ActualQty.Should().Be(8m);
        item1.DiffQty.Should().Be(-2m); // actual - system = 8 - 10 = -2

        var item2 = result.Value.Items.First(i => i.SkuId == sku2.Id);
        item2.SystemQty.Should().Be(5m);
        item2.ActualQty.Should().Be(12m);
        item2.DiffQty.Should().Be(7m); // actual - system = 12 - 5 = +7
    }

    [Fact]
    public async Task ApproveStockTake_ShouldFail_WhenUserIsNotManager()
    {
        // Acceptance Criteria: chỉ Quản lý mới có quyền phê duyệt
        // Arrange
        _currentUser.EmployeeId.Returns(_cashierId);
        _currentUser.StoreId.Returns(_storeId);

        var stockTake = Domain.Inventory.StockTake.StockTake.Create(_storeId, _cashierId, "Kiểm kê");
        stockTake.UpdateItem(Guid.NewGuid(), 10m, 5m);
        _stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, Arg.Any<CancellationToken>()).Returns(stockTake);

        var handler = new ApproveStockTakeCommandHandler(
            _stockTakeRepository, _stockEntryRepository, _stockTransactionRepository,
            _employeeRepository, _unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(new ApproveStockTakeCommand(stockTake.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StockTakeErrors.ManagerRequired.Code);
    }

    [Fact]
    public async Task ApproveStockTake_ShouldSucceed_AndAdjustInventory_WhenUserIsManager()
    {
        // Acceptance Criteria: Quản lý duyệt chênh lệch để tự động cân bằng tồn kho (Adjust)
        // Arrange
        _currentUser.EmployeeId.Returns(_managerId);
        _currentUser.StoreId.Returns(_storeId);

        var skuId = Guid.NewGuid();
        var stockTake = Domain.Inventory.StockTake.StockTake.Create(_storeId, _cashierId, "Kiểm kê kho");
        stockTake.UpdateItem(skuId, actualQty: 15m, systemQty: 10m, note: "Thừa 5 món");
        _stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, Arg.Any<CancellationToken>()).Returns(stockTake);

        var stockEntry = new StockEntry(_storeId, skuId, qtyOnHand: 10m);
        _stockEntryRepository.GetBySkuAndStoreAsync(skuId, _storeId, Arg.Any<CancellationToken>()).Returns(stockEntry);

        var handler = new ApproveStockTakeCommandHandler(
            _stockTakeRepository, _stockEntryRepository, _stockTransactionRepository,
            _employeeRepository, _unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(new ApproveStockTakeCommand(stockTake.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(StockTakeStatus.Approved.ToString());
        result.Value.ApprovedBy.Should().Be(_managerId);
        result.Value.ApprovedAt.Should().NotBeNull();

        // 1. Tồn kho được cân bằng theo số lượng thực tế
        stockEntry.QtyOnHand.Should().Be(15m);

        // 2. Tạo StockTransaction loại Adjust với Qty là chênh lệch (+5)
        await _stockTransactionRepository.Received(1).AddAsync(Arg.Is<StockTransaction>(tx =>
            tx.StoreId == _storeId &&
            tx.SkuId == skuId &&
            tx.Type == StockTransactionType.Adjust &&
            tx.Qty == 5m &&
            tx.CreatedBy == _managerId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitStockTake_ShouldChangeStatusToPending()
    {
        // Arrange
        _currentUser.EmployeeId.Returns(_cashierId);
        _currentUser.StoreId.Returns(_storeId);

        var stockTake = Domain.Inventory.StockTake.StockTake.Create(_storeId, _cashierId, "Kiểm kê xong");
        stockTake.UpdateItem(Guid.NewGuid(), 20m, 20m);
        _stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, Arg.Any<CancellationToken>()).Returns(stockTake);

        var handler = new SubmitStockTakeCommandHandler(_stockTakeRepository, _employeeRepository, _unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(new SubmitStockTakeCommand(stockTake.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(StockTakeStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetStockTakeById_ShouldReturnDetailDto()
    {
        // Arrange
        _currentUser.EmployeeId.Returns(_managerId);
        _currentUser.StoreId.Returns(_storeId);

        var stockTake = Domain.Inventory.StockTake.StockTake.Create(_storeId, _cashierId, "Phiếu A");
        _stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, Arg.Any<CancellationToken>()).Returns(stockTake);

        var handler = new GetStockTakeByIdQueryHandler(_stockTakeRepository, _currentUser);

        // Act
        var result = await handler.Handle(new GetStockTakeByIdQuery(stockTake.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(stockTake.Id);
        result.Value.StoreId.Should().Be(_storeId);
    }
}
