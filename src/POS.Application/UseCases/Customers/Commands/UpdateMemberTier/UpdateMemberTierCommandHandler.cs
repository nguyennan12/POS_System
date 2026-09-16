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
    public async Task<Result<MemberTierDto>> Handle(
        UpdateMemberTierCommand request,
        CancellationToken cancellationToken)
    {
        var tier = await memberTierRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tier is null)
        {
            return CustomerErrors.MemberTierNotFound;
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
