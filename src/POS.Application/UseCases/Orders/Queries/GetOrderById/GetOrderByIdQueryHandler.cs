using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Domain.Common;

namespace POS.Application.UseCases.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler(
    IOrderRepository orderRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeStoreAccessRepository employeeStoreAccessRepository,
    ICurrentUser currentUser) : IQueryHandler<GetOrderByIdQuery, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return OrderErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return OrderErrors.Unauthorized;

        var order = await orderRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken);
        if (order is null)
            return OrderErrors.OrderNotFound;

        if (!employee.IsChainOwner && employee.StoreId != order.StoreId)
        {
            var hasAccess = await employeeStoreAccessRepository.ExistsAsync(employee.Id, order.StoreId, cancellationToken);
            if (!hasAccess)
                return OrderErrors.InvalidStore;
        }

        return Result<OrderDetailDto>.Success(order.ToDetailDto());
    }
}
