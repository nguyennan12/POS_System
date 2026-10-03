using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Commands.DeletePromotion;

public class DeletePromotionCommandHandler(
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePromotionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeletePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.NotFound;
        }

        promotion.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
