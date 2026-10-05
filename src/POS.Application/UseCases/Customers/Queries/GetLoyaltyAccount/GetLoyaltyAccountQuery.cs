using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;

///  
/// Requests the loyalty balance and tier benefits for a customer.
/// </summary>
public record GetLoyaltyAccountQuery(Guid CustomerId) : IQuery<LoyaltyAccountDto>, IRequirePermission
{
    public string RequiredPermission => "customers:read";
}
