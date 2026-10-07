using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Orders;

namespace POS.Application.UseCases.Orders.Queries.GetOrders;

public record GetOrdersQuery(OrderFilterRequest Filter)
    : IQuery<PagedResponse<OrderSummaryDto>>, IRequirePermission
{
    public string RequiredPermission => "orders:read";
}
