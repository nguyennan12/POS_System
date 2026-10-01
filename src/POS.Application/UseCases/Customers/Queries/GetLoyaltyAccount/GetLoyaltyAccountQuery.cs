using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

/// <summary>
/// Requests the loyalty balance and tier benefits for a customer.
/// </summary>
public record GetLoyaltyAccountQuery(Guid CustomerId) : IRequest<Result<LoyaltyAccountDto>>;
