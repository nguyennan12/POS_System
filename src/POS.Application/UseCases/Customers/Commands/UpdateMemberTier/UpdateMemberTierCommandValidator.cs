using FluentValidation;

namespace POS.Application.UseCases.Customers.Commands.UpdateMemberTier;

public class UpdateMemberTierCommandValidator : AbstractValidator<UpdateMemberTierCommand>
{
    public UpdateMemberTierCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID hạng thành viên không được để trống.");
        RuleFor(x => x.MinSpending).GreaterThanOrEqualTo(0).WithMessage("Mức chi tiêu tối thiểu không được âm.");
        RuleFor(x => x.PointRate).InclusiveBetween(0, 1).WithMessage("Tỷ lệ tích điểm phải từ 0 đến 1.");
        RuleFor(x => x.DiscountRate).InclusiveBetween(0, 1).WithMessage("Tỷ lệ giảm giá phải từ 0 đến 1.");
        RuleFor(x => x.DisplayColor).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.DisplayColor));
    }
}
