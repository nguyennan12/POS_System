using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Queries.GetMemberTiers;

public record GetMemberTiersQuery() : IQuery<List<MemberTierDto>>, IRequirePermission
{
    public string RequiredPermission => "customers:read";
}
