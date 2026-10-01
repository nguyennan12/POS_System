using FluentValidation;
using POS.Application.UseCases.Products.Commands.UpdateProduct;

namespace POS.Application.UseCases.Products.Validators;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private static readonly string[] ValidStatuses = { "Active", "Inactive", "Discontinued" };

    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID sản phẩm không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Danh mục sản phẩm không được để trống.");

        RuleFor(x => x.BaseUnit)
            .NotEmpty().WithMessage("Đơn vị cơ bản không được để trống.")
            .MaximumLength(50).WithMessage("Đơn vị cơ bản không được vượt quá 50 ký tự.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái sản phẩm không được để trống.")
            .Must(s => Enum.TryParse<POS.Domain.Products.Enums.ProductStatus>(s, true, out _))
            .WithMessage("Trạng thái sản phẩm không hợp lệ. Giá trị hợp lệ: Active, Inactive.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
            .When(x => x.Description is not null);
    }
}
