using FluentValidation;

namespace POS.Application.UseCases.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandValidator : AbstractValidator<DeleteSupplierCommand>
{
    public DeleteSupplierCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Id nhà cung cấp không hợp lệ.");
    }
}
