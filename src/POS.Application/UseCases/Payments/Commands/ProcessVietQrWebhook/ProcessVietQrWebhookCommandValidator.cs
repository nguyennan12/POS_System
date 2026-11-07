using FluentValidation;

namespace POS.Application.UseCases.Payments.Commands.ProcessVietQrWebhook;

public sealed class ProcessVietQrWebhookCommandValidator : AbstractValidator<ProcessVietQrWebhookCommand>
{
    public ProcessVietQrWebhookCommandValidator()
    {
        RuleFor(x => x.Payload).NotNull();
        RuleFor(x => x.RawBody).NotEmpty();
        RuleFor(x => x.Signature).NotEmpty().Length(64);
        When(x => x.Payload is not null, () =>
        {
            RuleFor(x => x.Payload.Amount).GreaterThan(0);
            RuleFor(x => x.Payload.TransactionId).GreaterThan(0);
            RuleFor(x => x.Payload.Content).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Payload.AccountNumber).NotEmpty().MaximumLength(100);
        });
    }
}
