using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.DeleteSku;

public record DeleteSkuCommand(Guid SkuId) : ICommand;
