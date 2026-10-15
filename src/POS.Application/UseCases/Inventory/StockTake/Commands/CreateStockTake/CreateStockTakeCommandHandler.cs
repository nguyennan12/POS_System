using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.StockTake;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.CreateStockTake;

public class CreateStockTakeCommandHandler(
    IStockTakeRepository stockTakeRepository,
    IStoreRepository storeRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CreateStockTakeCommand, StockTakeDetailDto>
{
    public async Task<Result<StockTakeDetailDto>> Handle(
        CreateStockTakeCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Xác thực nhân viên
        if (currentUser.EmployeeId is null)
            return StockTakeErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockTakeErrors.Unauthorized;

        // 2. Xác định cửa hàng từ token
        if (currentUser.StoreId is null)
            return StockTakeErrors.StoreRequired;

        var store = await storeRepository.GetByIdAsync(currentUser.StoreId.Value, cancellationToken);
        if (store is null || !store.IsActive)
            return StockTakeErrors.StoreInactive;

        // 3. Khởi tạo phiếu kiểm kê (Draft)
        var stockTake = Domain.Inventory.StockTake.StockTake.Create(
            storeId: store.Id,
            createdBy: employee.Id,
            note: command.Note);

        await stockTakeRepository.AddAsync(stockTake, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, cancellationToken);
        return Result<StockTakeDetailDto>.Success(created!.ToDetailDto());
    }
}
