using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Commands.RedeemPoints;

///  
/// Requests redemption of a positive point amount with an optional order reference and note.
/// </summary>
public record RedeemPointsCommand(
    Guid CustomerId,
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
) : IRequest<Result<LoyaltyAccountDto>>;
