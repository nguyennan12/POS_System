using FluentValidation;
using POS.Application.UseCases.Products.Commands.UpdateUnitConversion;

namespace POS.Application.UseCases.Products.Validators;

public class UpdateUnitConversionCommandValidator : AbstractValidator<UpdateUnitConversionCommand>
{
    public UpdateUnitConversionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID đơn vị quy đổi không được để trống.");

        RuleFor(x => x.UnitName)
            .NotEmpty().WithMessage("Tên đơn vị không được để trống.")
            .MaximumLength(50).WithMessage("Tên đơn vị không được vượt quá 50 ký tự.");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("Hệ số quy đổi phải lớn hơn 0.");

        RuleFor(x => x.SellPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá bán theo đơn vị quy đổi phải >= 0.");
    }
}
