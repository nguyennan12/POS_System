using FluentValidation;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoicePdf;

public sealed class GetInvoicePdfQueryValidator : AbstractValidator<GetInvoicePdfQuery>
{
    public GetInvoicePdfQueryValidator() => RuleFor(x => x.Id).NotEmpty();
}
