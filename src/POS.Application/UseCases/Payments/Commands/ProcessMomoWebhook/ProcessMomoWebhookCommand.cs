using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;

namespace POS.Application.UseCases.Payments.Commands.ProcessMomoWebhook;

public record ProcessMomoWebhookCommand(MomoWebhookDto Payload) : ICommand<WebhookAckDto>;
