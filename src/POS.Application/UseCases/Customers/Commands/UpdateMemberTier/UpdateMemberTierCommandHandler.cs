using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.UpdateMemberTier;

public class UpdateMemberTierCommandHandler(
    IMemberTierRepository memberTierRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateMemberTierCommand, MemberTierDto>
{
    /// <summary>
    /// Updates a member tier after validating that spending thresholds remain unique and ordered.
    /// </summary>
    public async Task<Result<MemberTierDto>> Handle(
        UpdateMemberTierCommand request,
        CancellationToken cancellationToken)
    {
        var tier = await memberTierRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tier is null)
        {
            return CustomerErrors.MemberTierNotFound;
        }

        var allTiers = await memberTierRepository.GetAllAsync(cancellationToken);

        // Build simulated list of tiers with proposed minSpending
        var simulatedTiers = allTiers
            .Select(t => (
                Tier: t,
                Name: t.Name,
                MinSpending: t.Id == request.Id ? request.MinSpending : t.MinSpending
            ))
            .OrderBy(x => x.Name) // Normal (0), Silver (1), Gold (2), VIP (3)
            .ToList();

        // 1. Kiểm tra trùng lặp minSpending
        var hasDuplicateMinSpending = simulatedTiers
            .GroupBy(x => x.MinSpending)
            .Any(g => g.Count() > 1);

        if (hasDuplicateMinSpending)
        {
            return CustomerErrors.MemberTierDuplicateMinSpending;
        }

        // 2. Kiểm tra thứ tự tăng dần theo phân cấp (Normal < Silver < Gold < VIP)
        for (int i = 1; i < simulatedTiers.Count; i++)
        {
            if (simulatedTiers[i].MinSpending <= simulatedTiers[i - 1].MinSpending)
            {
                return CustomerErrors.MemberTierInvalidOrder;
            }
        }

        tier.Update(
            minSpending: request.MinSpending,
            pointRate: request.PointRate,
            discountRate: request.DiscountRate,
            displayColor: request.DisplayColor);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MemberTierDto(
            Id: tier.Id,
            Name: tier.Name.ToString(),
            MinSpending: tier.MinSpending,
            PointRate: tier.PointRate,
            DiscountRate: tier.DiscountRate,
            DisplayColor: tier.DisplayColor);
    }
}
