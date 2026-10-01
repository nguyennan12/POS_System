using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

/// <summary>
/// Initializes the query handler with the customer repository.
/// </summary>
public class GetLoyaltyAccountQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetLoyaltyAccountQuery, Result<LoyaltyAccountDto>>
{
    /// <summary>
    /// Returns the customer's loyalty balance and tier benefits, or an error if the customer or loyalty account is missing.
    /// </summary>
    public async Task<Result<LoyaltyAccountDto>> Handle(GetLoyaltyAccountQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetEntityByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        var loyaltyAccount = await customerRepository.GetLoyaltyAccountWithTierAsync(request.CustomerId, cancellationToken);
        if (loyaltyAccount is null)
        {
            return CustomerErrors.LoyaltyAccountNotFound;
        }

        var tier = loyaltyAccount.Customer?.MemberTier ?? customer.MemberTier;

        return new LoyaltyAccountDto(
            loyaltyAccount.CustomerId,
            loyaltyAccount.PointsBalance,
            tier?.Name.ToString() ?? "Normal",
            tier?.PointRate ?? 0,
            tier?.DiscountRate ?? 0,
            loyaltyAccount.LastUpdated
        );
    }
}
