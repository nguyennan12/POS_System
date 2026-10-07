using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.RedeemPoints;

///  
/// Requests redemption of a positive point amount with an optional order reference and note.
/// </summary>
public record RedeemPointsCommand(
    Guid CustomerId,
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
) : ICommand<LoyaltyAccountDto>, IRequirePermission
{
    public string RequiredPermission => "customers:update";
}
