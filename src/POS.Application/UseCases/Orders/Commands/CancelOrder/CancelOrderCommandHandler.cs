using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeStoreAccessRepository employeeStoreAccessRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CancelOrderCommand, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        CancelOrderCommand command,
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

        if (!employee.IsChainOwner && employee.StoreId != order.StoreId)
        {
            var hasAccess = await employeeStoreAccessRepository.ExistsAsync(employee.Id, order.StoreId, cancellationToken);
            if (!hasAccess)
                return OrderErrors.InvalidStore;
        }

        if (order.Status == OrderStatus.Paid)
            return OrderErrors.CannotCancelPaidOrder;

        if (order.Status == OrderStatus.Cancelled)
            return OrderErrors.AlreadyCancelled;

        order.Cancel(command.Reason);
        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedOrder = await orderRepository.GetByIdWithDetailsAsync(order.Id, cancellationToken) ?? order;
        return Result<OrderDetailDto>.Success(updatedOrder.ToDetailDto());
    }
}
