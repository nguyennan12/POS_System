using FluentValidation;

namespace POS.Application.UseCases.Invoices.Queries.GetReceiptData;

public sealed class GetReceiptDataQueryValidator : AbstractValidator<GetReceiptDataQuery>
{
    public GetReceiptDataQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PaperWidth).Must(x => x is null or 58 or 80)
            .WithMessage("Khổ giấy phải là 58 hoặc 80 mm.");
    }
}
