using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Products;

namespace POS.Application.UseCases.Products.Commands.CreateUnitConversion;

public record CreateUnitConversionCommand(
    Guid SkuId,
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice
) : ICommand<UnitConversionDto>, IRequirePermission
{
    public string RequiredPermission => "Products.Create";
}
