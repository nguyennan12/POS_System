using FluentValidation;
using POS.Application.UseCases.Products.Commands.CreatePriceList;

namespace POS.Application.UseCases.Products.Validators;

public class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.SkuId)
            .NotEmpty().WithMessage("ID SKU không được để trống.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Giá bán phải >= 0.");

        RuleFor(x => x.ValidFrom)
            .NotEmpty().WithMessage("Ngày bắt đầu hiệu lực không được để trống.");

        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("Ngày kết thúc hiệu lực phải sau ngày bắt đầu.")
            .When(x => x.ValidTo.HasValue);

        RuleFor(x => x.CustomerGroup)
            .MaximumLength(100).WithMessage("Nhóm khách hàng không được vượt quá 100 ký tự.")
            .When(x => x.CustomerGroup is not null);
    }
}
