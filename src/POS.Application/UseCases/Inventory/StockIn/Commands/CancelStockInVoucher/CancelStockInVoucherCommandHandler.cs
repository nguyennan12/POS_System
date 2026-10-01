using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CancelStockInVoucher;

/// <summary>
/// Hủy phiếu nhập kho (chỉ được hủy khi ở trạng thái Draft).
/// Domain.Cancel() chặn Completed → Cancelled và Already-Cancelled.
/// </summary>
public class CancelStockInVoucherCommandHandler(
    IStockInVoucherRepository stockInVoucherRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CancelStockInVoucherCommand, StockInVoucherDetailDto>
{
    public async Task<Result<StockInVoucherDetailDto>> Handle(
        CancelStockInVoucherCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return StockInErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockInErrors.Unauthorized;

        var voucher = await stockInVoucherRepository.GetByIdWithItemsAsync(command.VoucherId, cancellationToken);
        if (voucher is null)
            return StockInErrors.VoucherNotFound;

        // Chỉ ChainOwner hoặc nhân viên cùng cửa hàng mới được hủy
        if (!employee.IsChainOwner && voucher.StoreId != employee.StoreId)
            return StockInErrors.AccessDenied;

        // Domain.Cancel() sẽ trả lỗi nếu Completed hoặc đã Cancelled
        var cancelResult = voucher.Cancel();
        if (cancelResult.IsFailure) return cancelResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await stockInVoucherRepository.GetByIdWithItemsAsync(voucher.Id, cancellationToken);
        return Result<StockInVoucherDetailDto>.Success(result!.ToDetailDto());
    }
}
