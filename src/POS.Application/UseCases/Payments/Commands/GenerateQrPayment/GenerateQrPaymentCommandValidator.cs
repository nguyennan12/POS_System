using FluentValidation;

namespace POS.Application.UseCases.Payments.Commands.GenerateQrPayment;

public sealed class GenerateQrPaymentCommandValidator : AbstractValidator<GenerateQrPaymentCommand>
{
    public GenerateQrPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Method).Must(x => x is "MoMo" or "VietQR").WithMessage("Phương thức QR không hợp lệ.");
    }
}
