using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Stock;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CompleteStockInVoucher;

///  
/// Hoàn thành phiếu nhập kho:
/// 1. Validate trạng thái phiếu (Draft → Completed).
/// 2. Thực thi trong Serializable transaction:
///    a. Với mỗi item: cộng dồn StockEntry.QtyOnHand + tính AverageCost qua domain method.
///    b. Tạo/cộng dồn StockBatch nếu item có BatchNo.
///    c. Tạo StockTransaction (StockIn) cho từng item.
///    d. SaveChangesAsync() một lần duy nhất ở cuối.
/// </summary>
public class CompleteStockInVoucherCommandHandler(
    IStockInVoucherRepository stockInVoucherRepository,
    IStockEntryRepository stockEntryRepository,
    IStockBatchRepository stockBatchRepository,
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

    // 3. Chỉ ChainOwner hoặc nhân viên thuộc cùng cửa hàng mới được complete
    if (!employee.IsChainOwner && voucher.StoreId != employee.StoreId)
      return StockInErrors.AccessDenied;

    // 4. Thực thi trong Serializable transaction
    return await unitOfWork.ExecuteSerializableAsync(async ct =>
    {
      // Reload để lấy trạng thái mới nhất trong transaction
      var freshVoucher = await stockInVoucherRepository.GetByIdWithItemsAsync(command.VoucherId, ct);
      if (freshVoucher is null) return StockInErrors.VoucherNotFound;

      // 5. Complete phiếu (Draft → Completed)
      var completeResult = freshVoucher.Complete();
      if (completeResult.IsFailure) return completeResult.Error;

      // 6. Với từng item: cập nhật StockEntry và StockBatch
      foreach (var item in freshVoucher.Items)
      {
        // 6a. Lấy hoặc tạo StockEntry
        var entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, freshVoucher.StoreId, ct);
        if (entry is null)
        {
          entry = new StockEntry(freshVoucher.StoreId, item.SkuId, 0);
          await stockEntryRepository.AddAsync(entry, ct);
        }

        // 6b. Cộng tồn kho + tính lại AverageCost qua Domain method (không raw SQL)
        entry.IncreaseStock(item.Qty, item.UnitPrice);

        // 6c. Tạo StockTransaction ledger entry
        var tx = StockTransaction.CreateStockIn(
                storeId: freshVoucher.StoreId,
                skuId: item.SkuId,
                qty: item.Qty,
                createdBy: employee.Id,
                stockInVoucherId: freshVoucher.Id,
                unitCost: item.UnitPrice);

        await stockTransactionRepository.AddAsync(tx, ct);

        // 6d. Nếu item có BatchNo thì tạo mới hoặc cộng dồn StockBatch
        if (!string.IsNullOrWhiteSpace(item.BatchNo))
        {
          var batch = await stockBatchRepository.GetByBatchNoAsync(
                  freshVoucher.StoreId, item.SkuId, item.BatchNo, ct);

          if (batch is null)
            await stockBatchRepository.AddAsync(
                    new StockBatch(freshVoucher.StoreId, item.SkuId, item.BatchNo, item.Qty, item.ExpiryDate),
                    ct);
          else
            batch.AddQty(item.Qty);
        }
      }

      // 7. Lưu tất cả thay đổi một lần
      await unitOfWork.SaveChangesAsync(ct);

      var result = await stockInVoucherRepository.GetByIdWithItemsAsync(freshVoucher.Id, ct);
      return Result<StockInVoucherDetailDto>.Success(result!.ToDetailDto());
    }, cancellationToken);
  }
}
