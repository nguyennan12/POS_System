using FluentValidation;
using POS.Application.UseCases.Products.Commands.CreateProduct;

namespace POS.Application.UseCases.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Danh mục sản phẩm không được để trống.");

        RuleFor(x => x.BaseUnit)
            .NotEmpty().WithMessage("Đơn vị cơ bản không được để trống.")
            .MaximumLength(50).WithMessage("Đơn vị cơ bản không được vượt quá 50 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Brand)
            .MaximumLength(100).WithMessage("Thương hiệu không được vượt quá 100 ký tự.")
            .When(x => x.Brand is not null);

        RuleForEach(x => x.Skus).ChildRules(sku =>
        {
            sku.RuleFor(s => s.SkuCode)
                .NotEmpty().WithMessage("Mã SKU không được để trống.")
                .MaximumLength(100).WithMessage("Mã SKU không được vượt quá 100 ký tự.");

            sku.RuleFor(s => s.Barcode)
                .NotEmpty().WithMessage("Mã vạch không được để trống.")
                .MaximumLength(100).WithMessage("Mã vạch không được vượt quá 100 ký tự.");

            sku.RuleFor(s => s.SellPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá bán phải >= 0.");

            sku.RuleFor(s => s.CostPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá vốn phải >= 0.");

            sku.RuleFor(s => s.TaxRate)
                .Must(r => new[] { 0m, 5m, 8m, 10m }.Contains(r))
                .WithMessage("Thuế suất VAT phải là 0, 5, 8, hoặc 10.");
        }).When(x => x.Skus is not null && x.Skus.Any());
    }
}
