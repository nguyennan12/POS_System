using FluentValidation;

namespace POS.Application.UseCases.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Id nhà cung cấp không hợp lệ.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên nhà cung cấp không được để trống.")
            .MaximumLength(200).WithMessage("Tên nhà cung cấp tối đa 200 ký tự.");

        RuleFor(x => x.TaxCode)
            .MaximumLength(20).WithMessage("Mã số thuế tối đa 20 ký tự.")
            .When(x => x.TaxCode is not null);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(100).WithMessage("Email tối đa 100 ký tự.")
            .When(x => x.Email is not null);

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Địa chỉ tối đa 500 ký tự.")
            .When(x => x.Address is not null);
    }
}
