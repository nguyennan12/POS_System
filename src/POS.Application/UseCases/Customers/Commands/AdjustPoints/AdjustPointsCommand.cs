using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Commands.AdjustPoints;

///  
/// Requests a signed point adjustment and its required explanatory note.
/// </summary>
public record AdjustPointsCommand(
    Guid CustomerId,
    decimal Points,
    string Note
) : IRequest<Result<LoyaltyAccountDto>>;
