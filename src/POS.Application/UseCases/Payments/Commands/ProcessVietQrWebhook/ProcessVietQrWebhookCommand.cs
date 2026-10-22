using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;

namespace POS.Application.UseCases.Payments.Commands.ProcessVietQrWebhook;

public record ProcessVietQrWebhookCommand(VietQrWebhookDto Payload, byte[] RawBody, string? Signature)
    : ICommand<WebhookAckDto>;
