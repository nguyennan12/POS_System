using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Stock;

namespace POS.Application.UseCases.Inventory.Stock.Commands.DisposeStock;

///  
/// Xuất hủy hàng hóa (Dispose):
/// 1. Kiểm tra tồn kho đủ số lượng hủy.
/// 2. Nếu có BatchId: kiểm tra và trừ StockBatch.Qty trước.
/// 3. Trừ StockEntry.QtyOnHand (atomic raw SQL).
/// 4. Tạo StockTransaction loại Dispose với UnitCost = AverageCost tại thời điểm hủy.
/// Thực thi trong Serializable transaction.
/// </summary>
public class DisposeStockCommandHandler(
    IStockEntryRepository stockEntryRepository,
    IStockBatchRepository stockBatchRepository,
    IStockTransactionRepository stockTransactionRepository,
    IEmployeeRepository employeeRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<DisposeStockCommand, StockTransactionDto>
{
  public async Task<Result<StockTransactionDto>> Handle(
      DisposeStockCommand command,
      CancellationToken cancellationToken)
  {
    if (currentUser.EmployeeId is null)
      return InventoryErrors.Unauthorized;

    var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
    if (employee is null || !employee.IsActive)
      return InventoryErrors.Unauthorized;

    if (currentUser.StoreId is null)
      return InventoryErrors.StoreRequired;

    if (employee.StoreId != currentUser.StoreId)
      return InventoryErrors.Unauthorized;

    var store = await storeRepository.GetByIdAsync(currentUser.StoreId.Value, cancellationToken);
    if (store is null || !store.IsActive)
      return InventoryErrors.StoreInactive;

    return await unitOfWork.ExecuteSerializableAsync(async ct =>
    {
      var entry = await stockEntryRepository.GetBySkuAndStoreAsync(command.SkuId, currentUser.StoreId.Value, ct);
      if (entry is null)
        return InventoryErrors.SkuNotFound(command.SkuId);

      if (entry.QtyOnHand < command.Qty)
        return InventoryErrors.InsufficientStock(command.SkuId, entry.QtyOnHand, command.Qty);

      // Lấy UnitCost tại thời điểm hủy = AverageCost hiện tại
      var unitCostAtDispose = entry.AverageCost;

      // Trừ StockBatch nếu có BatchId
      if (command.BatchId.HasValue)
      {
        var batch = await stockBatchRepository.GetByIdAsync(command.BatchId.Value, ct);
        if (batch is null || batch.SkuId != command.SkuId || batch.StoreId != currentUser.StoreId.Value)
          return InventoryErrors.BatchNotFound(command.BatchId.Value);

        if (batch.Qty < command.Qty)
          return InventoryErrors.InsufficientBatchStock(command.BatchId.Value, batch.Qty, command.Qty);

        batch.DeductQty(command.Qty);
      }

      // Trừ tổng tồn kho atomic (raw SQL)
      await stockEntryRepository.DeductStockAsync(command.SkuId, currentUser.StoreId.Value, command.Qty, ct);

      // Ghi ledger entry với UnitCost
      var tx = StockTransaction.CreateDispose(
              storeId: currentUser.StoreId.Value,
              skuId: command.SkuId,
              qty: command.Qty,
              createdBy: employee.Id,
              note: command.Note,
              unitCost: unitCostAtDispose);

      await stockTransactionRepository.AddAsync(tx, ct);
      await unitOfWork.SaveChangesAsync(ct);

      return Result<StockTransactionDto>.Success(tx.ToDto());
    }, cancellationToken);
  }
}
