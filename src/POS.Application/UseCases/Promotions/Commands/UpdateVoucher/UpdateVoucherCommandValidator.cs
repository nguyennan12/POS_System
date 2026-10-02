using FluentValidation;

namespace POS.Application.UseCases.Promotions.Commands.UpdateVoucher;

public class UpdateVoucherCommandValidator : AbstractValidator<UpdateVoucherCommand>
{
    public UpdateVoucherCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã voucher không được để trống.");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).WithMessage("Giới hạn lượt dùng (max_uses) phải lớn hơn 0.");

        RuleFor(x => x.PerCustomerLimit)
            .GreaterThan(0).WithMessage("Giới hạn lượt dùng cho mỗi khách hàng phải lớn hơn 0.");
    }
}
