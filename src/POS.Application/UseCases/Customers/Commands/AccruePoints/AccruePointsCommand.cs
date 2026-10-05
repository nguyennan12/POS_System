using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.AccruePoints;

///  
/// Requests a positive point accrual with an optional order reference and note.
/// </summary>
public record AccruePointsCommand(
    Guid CustomerId,
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
) : ICommand<LoyaltyAccountDto>, IRequirePermission
{
    public string RequiredPermission => "customers:update";
}
