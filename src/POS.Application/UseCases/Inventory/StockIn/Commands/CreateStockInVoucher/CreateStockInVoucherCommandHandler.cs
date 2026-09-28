using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.StockIn;
using POS.Domain.Inventory.Stock;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CreateStockInVoucher;

public class CreateStockInVoucherCommandHandler(
    IStockInVoucherRepository stockInVoucherRepository,
    ISkuRepository skuRepository,
    ISupplierRepository supplierRepository,
    IStoreRepository storeRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CreateStockInVoucherCommand, StockInVoucherDetailDto>
{
    public async Task<Result<StockInVoucherDetailDto>> Handle(
        CreateStockInVoucherCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Xác thực nhân viên
        if (currentUser.EmployeeId is null)
            return StockInErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockInErrors.Unauthorized;

        // 2. Xác định cửa hàng từ token
        if (currentUser.StoreId is null)
            return StockInErrors.StoreRequired;

        var store = await storeRepository.GetByIdAsync(currentUser.StoreId.Value, cancellationToken);
        if (store is null || !store.IsActive)
            return StockInErrors.StoreInactive;

        // 3. Kiểm tra nhà cung cấp
        var supplier = await supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);
        if (supplier is null)
            return StockInErrors.SupplierNotFound;

        // 4. Kiểm tra SKU items
        if (!command.Items.Any())
            return StockInErrors.NoItems;

        var skuIds = command.Items.Select(i => i.SkuId).Distinct();
        var skus = await skuRepository.GetByIdsWithProductAsync(skuIds, cancellationToken);
        var skuMap = skus.ToDictionary(s => s.Id);

        foreach (var item in command.Items)
        {
            if (!skuMap.ContainsKey(item.SkuId))
                return StockInErrors.SkuNotFound(item.SkuId);
        }

        // 5. Tạo phiếu nhập
        var voucher = StockInVoucher.Create(
            storeId: store.Id,
            supplierId: supplier.Id,
            createdBy: employee.Id,
            note: command.Note);

        foreach (var item in command.Items)
        {
            var addResult = voucher.AddItem(item.SkuId, item.Qty, item.UnitPrice);
            if (addResult.IsFailure) return addResult.Error;
        }

        await stockInVoucherRepository.AddAsync(voucher, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload với navigation properties để build DTO
        var created = await stockInVoucherRepository.GetByIdWithItemsAsync(voucher.Id, cancellationToken);
        return Result<StockInVoucherDetailDto>.Success(created!.ToDetailDto());
    }
}
