using FluentValidation;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotionById;

public class GetPromotionByIdQueryValidator : AbstractValidator<GetPromotionByIdQuery>
{
    public GetPromotionByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã khuyến mãi không được để trống.");
    }
}
