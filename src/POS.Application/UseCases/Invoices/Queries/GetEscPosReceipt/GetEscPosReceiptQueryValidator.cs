using FluentValidation;

namespace POS.Application.UseCases.Invoices.Queries.GetEscPosReceipt;

public sealed class GetEscPosReceiptQueryValidator : AbstractValidator<GetEscPosReceiptQuery>
{
    public GetEscPosReceiptQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PaperWidth).Must(x => x is null or 58 or 80)
            .WithMessage("Khổ giấy phải là 58 hoặc 80 mm.");
    }
}
