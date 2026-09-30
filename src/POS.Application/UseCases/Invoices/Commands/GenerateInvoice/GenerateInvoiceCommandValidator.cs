using FluentValidation;

namespace POS.Application.UseCases.Invoices.Commands.GenerateInvoice;

public sealed class GenerateInvoiceCommandValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng không được để trống.");
    }
}
