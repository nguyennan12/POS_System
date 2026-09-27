using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Orders.Commands.CancelOrder;

public record CancelOrderCommand(
    Guid OrderId,
    string? Reason = null
) : ICommand<OrderDetailDto>, IRequirePermission
{
  public string RequiredPermission => "orders:update";
}
