using FluentValidation;

namespace POS.Application.UseCases.Promotions.Commands.CreateVoucher;

public class CreateVoucherCommandValidator : AbstractValidator<CreateVoucherCommand>
{
    public CreateVoucherCommandValidator()
    {
        RuleFor(x => x.PromotionId)
            .NotEmpty().WithMessage("Mã chương trình khuyến mãi không được để trống.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã voucher không được để trống.")
            .MaximumLength(50).WithMessage("Mã voucher không được vượt quá 50 ký tự.");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).WithMessage("Giới hạn lượt dùng (max_uses) phải lớn hơn 0.");

        RuleFor(x => x.PerCustomerLimit)
            .GreaterThan(0).WithMessage("Giới hạn lượt dùng cho mỗi khách hàng phải lớn hơn 0.")
            .LessThanOrEqualTo(x => x.MaxUses).WithMessage("Giới hạn lượt dùng cho mỗi khách hàng không được vượt quá tổng lượt dùng tối đa.");
    }
}
