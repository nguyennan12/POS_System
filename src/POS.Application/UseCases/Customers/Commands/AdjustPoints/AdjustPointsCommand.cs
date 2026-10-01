using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Commands.AdjustPoints;

public record AdjustPointsCommand(
    Guid CustomerId,
    decimal Points,
    string Note
) : IRequest<Result<LoyaltyAccountDto>>;
