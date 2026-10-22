using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Commands.ProcessVietQrWebhook;

public sealed class ProcessVietQrWebhookCommandHandler(QrWebhookProcessor processor)
    : ICommandHandler<ProcessVietQrWebhookCommand, WebhookAckDto>
{
    public Task<Result<WebhookAckDto>> Handle(ProcessVietQrWebhookCommand command, CancellationToken ct) =>
        processor.ProcessAsync(PaymentMethod.VietQR,
            new(VietQr: command.Payload, RawBody: command.RawBody, Signature: command.Signature), ct);
}
