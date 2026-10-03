using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.AdjustPoints;

public record AdjustPointsCommand(
    Guid CustomerId,
    decimal Points,
    string Note
) : ICommand<LoyaltyAccountDto>, IRequirePermission
{
    public string RequiredPermission => "customers:update";
}
