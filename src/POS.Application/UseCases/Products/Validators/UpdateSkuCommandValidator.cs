using FluentValidation;
using POS.Application.UseCases.Products.Commands.UpdateSku;

namespace POS.Application.UseCases.Products.Validators;

public class UpdateSkuCommandValidator : AbstractValidator<UpdateSkuCommand>
{
    public UpdateSkuCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID SKU không được để trống.");

        RuleFor(x => x.SkuCode)
            .NotEmpty().WithMessage("Mã SKU không được để trống.")
            .MaximumLength(100).WithMessage("Mã SKU không được vượt quá 100 ký tự.");

        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Mã vạch không được để trống.")
            .MaximumLength(100).WithMessage("Mã vạch không được vượt quá 100 ký tự.");

        RuleFor(x => x.SellPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá bán phải >= 0.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá vốn phải >= 0.");

        RuleFor(x => x.TaxRate)
            .Must(r => new[] { 0m, 5m, 8m, 10m }.Contains(r))
            .WithMessage("Thuế suất VAT phải là 0, 5, 8, hoặc 10.");
    }
}
