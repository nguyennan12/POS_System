using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Stock;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CompleteStockInVoucher;

/// <summary>
/// Hoàn thành phiếu nhập kho:
/// 1. Validate trạng thái phiếu (Draft → Completed).
/// 2. Thực thi trong Serializable transaction:
///    a. Tạo StockTransaction (StockIn) cho từng item.
///    b. Tăng QtyOnHand và tính lại AverageCost (Weighted Average Cost).
///    c. Nếu StockEntry chưa tồn tại, tạo mới.
/// </summary>
public class CompleteStockInVoucherCommandHandler(
    IStockInVoucherRepository stockInVoucherRepository,
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CompleteStockInVoucherCommand, StockInVoucherDetailDto>
{
    public async Task<Result<StockInVoucherDetailDto>> Handle(
        CompleteStockInVoucherCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Xác thực nhân viên
        if (currentUser.EmployeeId is null)
            return StockInErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockInErrors.Unauthorized;

        // 2. Lấy phiếu nhập
        var voucher = await stockInVoucherRepository.GetByIdWithItemsAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return StockInErrors.VoucherNotFound;

        // 3. Chỉ người tạo hoặc ChainOwner mới được complete
        if (!employee.IsChainOwner && voucher.CreatedBy != employee.Id)
            return StockInErrors.AccessDenied;

        // 4. Thực thi trong Serializable transaction để đảm bảo an toàn
        return await unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            // Reload để lấy trạng thái mới nhất trong transaction
            var freshVoucher = await stockInVoucherRepository.GetByIdWithItemsAsync(command.VoucherId, ct);
            if (freshVoucher is null) return StockInErrors.VoucherNotFound;

            // 5. Complete phiếu
            var completeResult = freshVoucher.Complete();
            if (completeResult.IsFailure) return completeResult.Error;

            // 6. Với từng item: tạo StockTransaction + cập nhật StockEntry
            foreach (var item in freshVoucher.Items)
            {
                // 6a. Lấy hoặc tạo StockEntry
                var entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, freshVoucher.StoreId, ct);
                if (entry is null)
                {
                    entry = new StockEntry(freshVoucher.StoreId, item.SkuId, 0);
                    await stockEntryRepository.AddAsync(entry, ct);
                    await unitOfWork.SaveChangesAsync(ct); // flush để có Id
                    entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, freshVoucher.StoreId, ct);
                }

                // 6b. Tính giá vốn bình quân mới
                var totalValue = entry!.QtyOnHand * entry.AverageCost + item.Qty * item.UnitPrice;
                var newQty = entry.QtyOnHand + item.Qty;
                var newAvgCost = newQty > 0 ? totalValue / newQty : item.UnitPrice;

                // 6c. Cập nhật tồn kho atomic bằng raw SQL
                await stockEntryRepository.IncrementStockAsync(item.SkuId, freshVoucher.StoreId, item.Qty, newAvgCost, ct);

                // 6d. Tạo StockTransaction ledger entry
                var tx = StockTransaction.CreateStockIn(
                    storeId: freshVoucher.StoreId,
                    skuId: item.SkuId,
                    qty: item.Qty,
                    createdBy: employee.Id,
                    stockInVoucherId: freshVoucher.Id,
                    unitCost: item.UnitPrice);

                await stockTransactionRepository.AddAsync(tx, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);

            var result = await stockInVoucherRepository.GetByIdWithItemsAsync(freshVoucher.Id, ct);
            return Result<StockInVoucherDetailDto>.Success(result!.ToDetailDto());
        }, cancellationToken);
    }
}
