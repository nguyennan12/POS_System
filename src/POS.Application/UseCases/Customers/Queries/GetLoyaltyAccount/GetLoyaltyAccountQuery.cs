using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

public record GetLoyaltyAccountQuery(Guid CustomerId) : IQuery<LoyaltyAccountDto>, IRequirePermission
{
    public string RequiredPermission => "customers:read";
}
