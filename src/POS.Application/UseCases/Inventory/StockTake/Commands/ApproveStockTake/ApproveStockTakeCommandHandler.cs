using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.ApproveStockTake;

/// <summary>
/// Quản lý duyệt chênh lệch kiểm kê kho:
/// 1. Chỉ Quản lý (StoreManager / Owner / ChainOwner) mới có quyền duyệt.
/// 2. Chuyển trạng thái sang Approved và gán ApprovedBy, ApprovedAt.
/// 3. Cân bằng tồn kho (Adjust):
///    - Cập nhật StockEntry.QtyOnHand = ActualQty.
///    - Nếu DiffQty != 0: ghi nhận StockTransaction loại Adjust.
/// </summary>
public class ApproveStockTakeCommandHandler(
    IStockTakeRepository stockTakeRepository,
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<ApproveStockTakeCommand, StockTakeDetailDto>
{
    public async Task<Result<StockTakeDetailDto>> Handle(
        ApproveStockTakeCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Xác thực nhân viên
        if (currentUser.EmployeeId is null)
            return StockTakeErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockTakeErrors.Unauthorized;

        // 2. Chỉ Quản lý mới có quyền phê duyệt
        var isManager = employee.IsChainOwner || employee.Role?.Name is RoleNames.StoreManager or RoleNames.Owner;
        if (!isManager)
            return StockTakeErrors.ManagerRequired;

        // 3. Lấy phiếu kiểm kê
        var stockTake = await stockTakeRepository.GetByIdWithItemsAsync(command.StockTakeId, cancellationToken);
        if (stockTake is null)
            return StockTakeErrors.NotFound;

        // 4. Phân quyền theo cửa hàng
        if (!employee.IsChainOwner && stockTake.StoreId != employee.StoreId)
            return StockTakeErrors.AccessDenied;

        // 5. Kiểm tra trạng thái và số lượng item
        if (stockTake.Status == StockTakeStatus.Approved)
            return StockTakeErrors.AlreadyApproved;

        if (!stockTake.Items.Any())
            return StockTakeErrors.NoItems;

        // 6. Thực thi trong Serializable transaction
        return await unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            var freshTake = await stockTakeRepository.GetByIdWithItemsAsync(command.StockTakeId, ct);
            if (freshTake is null)
                return StockTakeErrors.NotFound;

            var approveResult = freshTake.Approve(employee.Id);
            if (approveResult.IsFailure)
                return approveResult.Error;

            // 7. Cân bằng tồn kho cho từng item
            foreach (var item in freshTake.Items)
            {
                var entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, freshTake.StoreId, ct);
                if (entry is null)
                {
                    entry = new StockEntry(freshTake.StoreId, item.SkuId, item.ActualQty);
                    await stockEntryRepository.AddAsync(entry, ct);
                }
                else
                {
                    entry.AdjustStock(item.ActualQty);
                }

                // Nếu có chênh lệch, tạo StockTransaction loại Adjust
                if (item.DiffQty != 0)
                {
                    var tx = StockTransaction.CreateAdjust(
                        storeId: freshTake.StoreId,
                        skuId: item.SkuId,
                        qty: item.DiffQty,
                        createdBy: employee.Id,
                        note: item.Note ?? $"Điều chỉnh kiểm kê kho (chênh lệch: {item.DiffQty:+0.###;-0.###;0})",
                        unitCost: entry.AverageCost);

                    await stockTransactionRepository.AddAsync(tx, ct);
                }
            }

            await unitOfWork.SaveChangesAsync(ct);

            var result = await stockTakeRepository.GetByIdWithItemsAsync(freshTake.Id, ct);
            return Result<StockTakeDetailDto>.Success(result!.ToDetailDto());
        }, cancellationToken);
    }
}
