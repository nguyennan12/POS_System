using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid ShiftId,
    Guid? CustomerId = null,
    string? Note = null
) : ICommand<OrderDetailDto>, IRequirePermission
{
    public string RequiredPermission => "orders:create";
}
