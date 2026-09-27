using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(
    Guid Id
) : IQuery<OrderDetailDto>, IRequirePermission
{
    public string RequiredPermission => "orders:read";
}
