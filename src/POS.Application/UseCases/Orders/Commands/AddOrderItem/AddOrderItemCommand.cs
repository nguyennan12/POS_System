using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Orders.Commands.AddOrderItem;

public record AddOrderItemCommand(
    Guid OrderId,
    Guid SkuId,
    decimal Qty
) : ICommand<OrderDetailDto>, IRequirePermission
{
    public string RequiredPermission => "orders:update";
}
