using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Commands.AccruePoints;

public record AccruePointsCommand(
    Guid CustomerId,
    decimal Points,
    Guid? OrderId = null,
    string? Note = null
) : IRequest<Result<LoyaltyAccountDto>>;
