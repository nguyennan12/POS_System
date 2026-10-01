using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Domain.Common;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders;

namespace POS.Application.UseCases.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CreateOrderCommand, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return OrderErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return OrderErrors.Unauthorized;

        var shift = await shiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift is null)
            return OrderErrors.ShiftNotFound;

        if (shift.Status != ShiftStatus.Open)
            return OrderErrors.ShiftClosed;

        // Kiểm tra quyền cửa hàng (ChainOwner hoặc cùng cửa hàng)
        if (!employee.IsChainOwner && employee.StoreId != shift.StoreId)
        {
            return OrderErrors.InvalidStore;
        }

        if (command.CustomerId.HasValue)
        {
            var customerWithPoints = await customerRepository.GetByIdAsync(command.CustomerId.Value, cancellationToken);
            if (customerWithPoints is null)
                return OrderErrors.CustomerNotFound;
        }

        var order = Order.CreateDraft(
            storeId: shift.StoreId,
            shiftId: shift.Id,
            createdBy: employee.Id,
            customerId: command.CustomerId,
            currencyCode: "VND",
            note: command.Note);

        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var createdOrder = await orderRepository.GetByIdWithDetailsAsync(order.Id, cancellationToken);
        return Result<OrderDetailDto>.Success(createdOrder!.ToDetailDto());
    }
}
