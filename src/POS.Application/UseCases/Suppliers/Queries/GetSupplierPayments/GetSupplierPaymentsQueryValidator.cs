using FluentValidation;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierPayments;

public class GetSupplierPaymentsQueryValidator : AbstractValidator<GetSupplierPaymentsQuery>
{
    public GetSupplierPaymentsQueryValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Id nhà cung cấp không hợp lệ.");
    }
}
