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
using POS.Domain.Promotions;

namespace POS.Application.UseCases.Orders.Commands.AddOrderItem;

public class AddOrderItemCommandHandler(
    IOrderRepository orderRepository,
    ISkuRepository skuRepository,
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    IVoucherRepository voucherRepository,
    ICartCalculationService cartCalculationService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<AddOrderItemCommand, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        AddOrderItemCommand command,
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

        // Kiểm tra quyền cửa hàng của nhân viên
        if (!employee.IsChainOwner && employee.StoreId != order.StoreId)
        {
            return OrderErrors.InvalidStore;
        }

        var shift = await shiftRepository.GetByIdAsync(order.ShiftId, cancellationToken);
        if (shift is null || shift.Status != ShiftStatus.Open)
            return OrderErrors.ShiftClosed;

        var sku = await skuRepository.GetByIdWithProductAsync(command.SkuId, cancellationToken);
        if (sku is null)
            return OrderErrors.SkuNotFound;

        // Ràng buộc SKU phải thuộc đúng Store của đơn hàng
        if (sku.StoreId != order.StoreId)
            return OrderErrors.SkuNotFound;

        if (!sku.IsActive)
            return OrderErrors.SkuInactive;

        // 1. Thêm / cập nhật SKU vào giỏ hàng
        order.AddOrUpdateItem(sku, command.Qty);

        // 2. Tìm voucher hiện có trên đơn (nếu đã từng áp dụng)
        Voucher? currentVoucher = null;
        var voucherDiscount = order.Discounts.FirstOrDefault(d => d.VoucherId.HasValue);
        if (voucherDiscount?.VoucherId != null)
        {
            currentVoucher = await voucherRepository.GetByIdWithPromotionAsync(voucherDiscount.VoucherId.Value, cancellationToken);
        }

        // 3. Tự động tính toán lại giỏ hàng (Promotions + Tax + Totals)
        var recalcResult = await cartCalculationService.RecalculateAsync(order, currentVoucher, cancellationToken);
        if (recalcResult.IsFailure)
            return recalcResult.Error;

        // 4. Lưu dữ liệu
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Reload đầy đủ quan hệ để mapping sang DTO
        var updatedOrder = await orderRepository.GetByIdWithDetailsAsync(order.Id, cancellationToken);
        return Result<OrderDetailDto>.Success(updatedOrder!.ToDetailDto());
    }
}
