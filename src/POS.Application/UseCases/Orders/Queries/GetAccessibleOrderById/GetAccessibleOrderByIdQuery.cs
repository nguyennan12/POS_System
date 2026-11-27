using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Access;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Domain.Common;

namespace POS.Application.UseCases.Orders.Queries.GetAccessibleOrderById;

public sealed record GetAccessibleOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailDto>;

public sealed class GetAccessibleOrderByIdQueryHandler(
    IOrderReadAccessPolicy orderReadAccessPolicy,
    IOrderRepository orderRepository)
    : IQueryHandler<GetAccessibleOrderByIdQuery, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        GetAccessibleOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        var scopeResult = await orderReadAccessPolicy.ResolveReadScopeAsync(cancellationToken);
        if (scopeResult.IsFailure)
        {
            return scopeResult.Error;
        }

        var scope = scopeResult.Value;
        var order = await orderRepository.GetByIdWithDetailsAsync(query.OrderId, cancellationToken);
        if (order is null || order.StoreId != scope.StoreId)
        {
            return OrderErrors.OrderNotFound;
        }

        if (!scope.CanReadAllInStore && order.CreatedBy != scope.EmployeeId)
        {
            return new Error(
                ErrorType.Forbidden,
                "Order.ForbiddenOwnOnly",
                "Bạn chỉ có quyền xem đơn hàng do chính mình tạo.");
        }

        return Result<OrderDetailDto>.Success(order.ToDetailDto());
    }
}
