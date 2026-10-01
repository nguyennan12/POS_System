using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Orders.Commands.CheckoutOrder;

public record PaymentSplitInputDto(
    string Method,
    decimal Amount,
    string? TransactionRef = null
);

public record CheckoutOrderCommand(
    Guid OrderId,
    IReadOnlyList<PaymentSplitInputDto> Payments,
    Guid? CustomerId = null
) : ICommand<CheckoutDto>, IRequirePermission
{
    public string RequiredPermission => "orders:update";
}
