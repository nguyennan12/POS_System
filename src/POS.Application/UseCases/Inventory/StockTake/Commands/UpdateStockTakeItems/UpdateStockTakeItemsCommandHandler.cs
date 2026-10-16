using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Enums;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.UpdateStockTakeItems;

public class UpdateStockTakeItemsCommandHandler(
    IStockTakeRepository stockTakeRepository,
    IStockEntryRepository stockEntryRepository,
    ISkuRepository skuRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<UpdateStockTakeItemsCommand, StockTakeDetailDto>
{
    public async Task<Result<StockTakeDetailDto>> Handle(
        UpdateStockTakeItemsCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Xác thực nhân viên
        if (currentUser.EmployeeId is null)
            return StockTakeErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockTakeErrors.Unauthorized;

        // 2. Lấy phiếu kiểm kê
        var stockTake = await stockTakeRepository.GetByIdWithItemsAsync(command.StockTakeId, cancellationToken);
        if (stockTake is null)
            return StockTakeErrors.NotFound;

        // 3. Phân quyền cửa hàng
        if (!employee.IsChainOwner && stockTake.StoreId != employee.StoreId)
            return StockTakeErrors.AccessDenied;

        // 4. Trạng thái phiếu
        if (stockTake.Status == StockTakeStatus.Approved)
            return StockTakeErrors.AlreadyApproved;

        // 5. Kiểm tra danh sách mặt hàng
        if (!command.Items.Any())
            return StockTakeErrors.NoItems;

        var skuIds = command.Items.Select(i => i.SkuId).Distinct();
        var skus = await skuRepository.GetByIdsWithProductAsync(skuIds, cancellationToken);
        var skuMap = skus.ToDictionary(s => s.Id);

        foreach (var item in command.Items)
        {
            if (!skuMap.ContainsKey(item.SkuId))
                return StockTakeErrors.SkuNotFound(item.SkuId);
        }

        // 6. Cập nhật số lượng đếm thực tế và tính diff_qty = actual_qty - system_qty
        foreach (var item in command.Items)
        {
            var entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, stockTake.StoreId, cancellationToken);
            var systemQty = entry?.QtyOnHand ?? 0m;

            var updateResult = stockTake.UpdateItem(item.SkuId, item.ActualQty, systemQty, item.Note);
            if (updateResult.IsFailure)
                return updateResult.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, cancellationToken);
        return Result<StockTakeDetailDto>.Success(updated!.ToDetailDto());
    }
}
