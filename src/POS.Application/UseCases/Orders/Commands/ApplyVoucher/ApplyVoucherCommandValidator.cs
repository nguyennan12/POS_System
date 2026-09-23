using FluentValidation;

namespace POS.Application.UseCases.Orders.Commands.ApplyVoucher;

public class ApplyVoucherCommandValidator : AbstractValidator<ApplyVoucherCommand>
{
    public ApplyVoucherCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng không được để trống.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã voucher không được để trống.")
            .MaximumLength(50).WithMessage("Mã voucher không được vượt quá 50 ký tự.");
    }
}
