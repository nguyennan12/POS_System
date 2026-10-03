using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.RedeemPoints;

public record RedeemPointsCommand(
    Guid CustomerId,
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
) : ICommand<LoyaltyAccountDto>, IRequirePermission
{
    public string RequiredPermission => "customers:update";
}
