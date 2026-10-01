using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

public class GetLoyaltyAccountQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetLoyaltyAccountQuery, Result<LoyaltyAccountDto>>
{
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
