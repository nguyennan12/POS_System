using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Contracts.V1.Common;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler(
    IOrderRepository orderRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IQueryHandler<GetOrdersQuery, PagedResponse<OrderSummaryDto>>
{
    public async Task<Result<PagedResponse<OrderSummaryDto>>> Handle(
        GetOrdersQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return OrderErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return OrderErrors.Unauthorized;

        var filter = query.Filter;
        Guid? effectiveStoreId;

        if (!employee.IsChainOwner)
        {
            if (filter.StoreId.HasValue && filter.StoreId.Value != employee.StoreId)
            {
                return OrderErrors.InvalidStore;
            }
            effectiveStoreId = employee.StoreId;
        }
        else
        {
            effectiveStoreId = filter.StoreId;
        }

        OrderStatus? orderStatus = null;
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<OrderStatus>(filter.Status, true, out var parsedStatus))
        {
            orderStatus = parsedStatus;
        }

        var (items, total) = await orderRepository.GetPagedAsync(
            effectiveStoreId,
            filter.ShiftId,
            orderStatus,
            filter.From,
            filter.To,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken);

        return new PagedResponse<OrderSummaryDto>(items, filter.PageNumber, filter.PageSize, total);
    }
}
