using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Products;

namespace POS.Application.UseCases.Products.Commands.CreatePriceList;

public record CreatePriceListCommand(
    Guid SkuId,
    decimal Price,
    DateTime ValidFrom,
    DateTime? ValidTo,
    string? CustomerGroup
) : ICommand<PriceListDto>, IRequirePermission
{
    public string RequiredPermission => "products:create";
}
