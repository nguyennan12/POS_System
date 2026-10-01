using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

public record GetLoyaltyAccountQuery(Guid CustomerId) : IRequest<Result<LoyaltyAccountDto>>;
