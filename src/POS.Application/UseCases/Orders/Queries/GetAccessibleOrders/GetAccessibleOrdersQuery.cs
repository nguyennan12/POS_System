using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Access;
using POS.Application.UseCases.Orders.DTOs;
using POS.Contracts.V1.Common;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Orders.Queries.GetAccessibleOrders;

public sealed record GetAccessibleOrdersQuery(
    OrderStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Limit = 20) : IQuery<PagedResponse<OrderSummaryDto>>;

public sealed class GetAccessibleOrdersQueryHandler(
    IOrderReadAccessPolicy orderReadAccessPolicy,
    IOrderRepository orderRepository)
    : IQueryHandler<GetAccessibleOrdersQuery, PagedResponse<OrderSummaryDto>>
{
    public async Task<Result<PagedResponse<OrderSummaryDto>>> Handle(
        GetAccessibleOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var scopeResult = await orderReadAccessPolicy.ResolveReadScopeAsync(cancellationToken);
        if (scopeResult.IsFailure)
        {
            return scopeResult.Error;
        }

        var scope = scopeResult.Value;
        var limit = Math.Clamp(query.Limit, 1, 50);
        var createdByFilter = scope.CanReadAllInStore ? (Guid?)null : scope.EmployeeId;

        var (items, total) = await orderRepository.GetPagedByScopeAsync(
            storeId: scope.StoreId,
            createdByEmployeeId: createdByFilter,
            status: query.Status,
            from: query.From,
            to: query.To,
            pageNumber: 1,
            pageSize: limit,
            cancellationToken: cancellationToken);

        return Result<PagedResponse<OrderSummaryDto>>.Success(
            new PagedResponse<OrderSummaryDto>(items, 1, limit, total));
    }
}
