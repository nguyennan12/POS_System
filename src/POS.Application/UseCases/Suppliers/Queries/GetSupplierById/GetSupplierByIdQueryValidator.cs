using FluentValidation;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierById;

public class GetSupplierByIdQueryValidator : AbstractValidator<GetSupplierByIdQuery>
{
    public GetSupplierByIdQueryValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Id nhà cung cấp không hợp lệ.");
    }
}
