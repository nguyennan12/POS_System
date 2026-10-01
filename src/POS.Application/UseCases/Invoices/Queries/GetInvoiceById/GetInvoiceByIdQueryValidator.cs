using FluentValidation;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryValidator : AbstractValidator<GetInvoiceByIdQuery>
{
    public GetInvoiceByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id hóa đơn không hợp lệ.");
    }
}
