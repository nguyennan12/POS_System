using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Commands.UpdateMemberTier;

public record UpdateMemberTierCommand(
    Guid Id,
    decimal MinSpending,
    decimal PointRate,
    decimal DiscountRate,
    string? DisplayColor = null
) : ICommand<MemberTierDto>, IRequirePermission
{
    public string RequiredPermission => "customers:update";
}
