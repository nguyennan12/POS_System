using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Commands.ProcessMomoWebhook;

public sealed class ProcessMomoWebhookCommandHandler(QrWebhookProcessor processor)
    : ICommandHandler<ProcessMomoWebhookCommand, WebhookAckDto>
{
    public Task<Result<WebhookAckDto>> Handle(ProcessMomoWebhookCommand command, CancellationToken ct) =>
        processor.ProcessAsync(PaymentMethod.MoMo, new(Momo: command.Payload), ct);
}
