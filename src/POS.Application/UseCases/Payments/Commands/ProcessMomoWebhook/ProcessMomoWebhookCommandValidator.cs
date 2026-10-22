using FluentValidation;

namespace POS.Application.UseCases.Payments.Commands.ProcessMomoWebhook;

public sealed class ProcessMomoWebhookCommandValidator : AbstractValidator<ProcessMomoWebhookCommand>
{
    public ProcessMomoWebhookCommandValidator()
    {
        RuleFor(x => x.Payload).NotNull();
        When(x => x.Payload is not null, () =>
        {
            RuleFor(x => x.Payload.Signature).NotEmpty().Length(64);
            RuleFor(x => x.Payload.OrderId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Payload.RequestId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Payload.Amount).GreaterThan(0);
            RuleFor(x => x.Payload.TransId).GreaterThan(0);
        });
    }
}
