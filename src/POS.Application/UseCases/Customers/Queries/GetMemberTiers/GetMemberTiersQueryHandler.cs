using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Queries.GetMemberTiers;

public class GetMemberTiersQueryHandler(IMemberTierRepository memberTierRepository)
    : IQueryHandler<GetMemberTiersQuery, List<MemberTierDto>>
{
    public async Task<Result<List<MemberTierDto>>> Handle(
        GetMemberTiersQuery request,
        CancellationToken cancellationToken)
    {
        var tiers = await memberTierRepository.GetAllAsync(cancellationToken);

        return tiers.Select(t => new MemberTierDto(
            Id: t.Id,
            Name: t.Name.ToString(),
            MinSpending: t.MinSpending,
            PointRate: t.PointRate,
            DiscountRate: t.DiscountRate,
            DisplayColor: t.DisplayColor
        )).ToList();
    }
}
