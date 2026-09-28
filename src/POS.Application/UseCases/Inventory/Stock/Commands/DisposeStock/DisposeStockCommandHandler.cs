using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Stock;

namespace POS.Application.UseCases.Inventory.Stock.Commands.DisposeStock;

/// <summary>
/// Xuất hủy hàng hóa (Dispose):
/// 1. Kiểm tra tồn kho đủ số lượng hủy.
/// 2. Trừ kho atomic bằng raw SQL.
/// 3. Tạo StockTransaction loại Dispose.
/// Thực thi trong Serializable transaction.
/// </summary>
public class DisposeStockCommandHandler(
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    IEmployeeRepository employeeRepository,
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

        return await unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            var entry = await stockEntryRepository.GetBySkuAndStoreAsync(command.SkuId, currentUser.StoreId.Value, ct);
            if (entry is null)
                return InventoryErrors.SkuNotFound(command.SkuId);

            if (entry.QtyOnHand < command.Qty)
                return InventoryErrors.InsufficientStock(command.SkuId, entry.QtyOnHand, command.Qty);

            // Trừ kho atomic
            await stockEntryRepository.DeductStockAsync(command.SkuId, currentUser.StoreId.Value, command.Qty, ct);

            // Ghi ledger entry
            var tx = StockTransaction.CreateDispose(
                storeId: currentUser.StoreId.Value,
                skuId: command.SkuId,
                qty: command.Qty,
                createdBy: employee.Id,
                note: command.Note);

            await stockTransactionRepository.AddAsync(tx, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<StockTransactionDto>.Success(tx.ToDto());
        }, cancellationToken);
    }
}
