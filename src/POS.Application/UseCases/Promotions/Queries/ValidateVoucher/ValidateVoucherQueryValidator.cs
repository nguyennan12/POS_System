using FluentValidation;

namespace POS.Application.UseCases.Promotions.Queries.ValidateVoucher;

public class ValidateVoucherQueryValidator : AbstractValidator<ValidateVoucherQuery>
{
    public ValidateVoucherQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã voucher không được để trống.");

        RuleFor(x => x.OrderSubtotal)
            .GreaterThanOrEqualTo(0).WithMessage("Tổng tiền đơn hàng không được âm.");
    }
}
