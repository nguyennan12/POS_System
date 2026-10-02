using FluentValidation;

namespace POS.Application.UseCases.Promotions.Queries.GetVoucherById;

public class GetVoucherByIdQueryValidator : AbstractValidator<GetVoucherByIdQuery>
{
    public GetVoucherByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã voucher không được để trống.");
    }
}
