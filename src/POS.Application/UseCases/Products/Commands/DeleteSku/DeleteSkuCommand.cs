using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.DeleteSku;

public record DeleteSkuCommand(Guid SkuId) : ICommand, IRequirePermission
{
    public string RequiredPermission => "skus:manage";
}
