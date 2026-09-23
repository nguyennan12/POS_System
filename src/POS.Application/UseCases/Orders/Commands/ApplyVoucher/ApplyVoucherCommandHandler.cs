using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Application.UseCases.Orders.Services;
using POS.Domain.Common;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Orders.Commands.ApplyVoucher;

public class ApplyVoucherCommandHandler(
    IOrderRepository orderRepository,
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeStoreAccessRepository employeeStoreAccessRepository,
    IVoucherRepository voucherRepository,
    ICartCalculationService cartCalculationService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<ApplyVoucherCommand, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        ApplyVoucherCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return OrderErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return OrderErrors.Unauthorized;

        var order = await orderRepository.GetByIdWithDetailsAsync(command.OrderId, cancellationToken);
        if (order is null)
            return OrderErrors.OrderNotFound;

        if (order.Status != OrderStatus.Draft)
            return OrderErrors.NotDraft;

        if (order.Items.Count == 0)
            return OrderErrors.CartEmpty;

        // Kiểm tra quyền cửa hàng của nhân viên
        if (!employee.IsChainOwner && employee.StoreId != order.StoreId)
        {
            var hasAccess = await employeeStoreAccessRepository.ExistsAsync(employee.Id, order.StoreId, cancellationToken);
            if (!hasAccess)
                return OrderErrors.InvalidStore;
        }

        var shift = await shiftRepository.GetByIdAsync(order.ShiftId, cancellationToken);
        if (shift is null || shift.Status != ShiftStatus.Open)
            return OrderErrors.ShiftClosed;

        var voucher = await voucherRepository.GetByCodeWithPromotionAsync(command.Code, cancellationToken);
        if (voucher is null)
            return OrderErrors.VoucherNotFound;

        if (!voucher.IsActive)
            return OrderErrors.VoucherInactive;

        // Ràng buộc voucher/khuyến mãi phải hợp lệ cho Store của đơn hàng
        if (voucher.Promotion != null && voucher.Promotion.StoreId != Guid.Empty && voucher.Promotion.StoreId != order.StoreId)
            return OrderErrors.VoucherNotFound;

        var now = DateTime.UtcNow;
        if (voucher.ExpiresAt.HasValue && now > voucher.ExpiresAt.Value)
            return OrderErrors.VoucherExpired;

        if (voucher.UsedCount >= voucher.MaxUses)
            return OrderErrors.VoucherUsageLimitReached;

        int customerUsedCount = 0;
        if (order.CustomerId.HasValue)
        {
            customerUsedCount = await voucherRepository.GetCustomerUsageCountAsync(voucher.Id, order.CustomerId.Value, cancellationToken);
        }

        if (!voucher.CanBeUsed(now, customerUsedCount))
            return OrderErrors.VoucherCustomerLimitReached;

        // Tự động tính lại giỏ hàng kèm Voucher
        var recalcResult = await cartCalculationService.RecalculateAsync(order, voucher, cancellationToken);
        if (recalcResult.IsFailure)
            return recalcResult.Error;

        // Lưu dữ liệu
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload đầy đủ quan hệ để mapping sang DTO
        var updatedOrder = await orderRepository.GetByIdWithDetailsAsync(order.Id, cancellationToken);
        return Result<OrderDetailDto>.Success(updatedOrder!.ToDetailDto());
    }
}
