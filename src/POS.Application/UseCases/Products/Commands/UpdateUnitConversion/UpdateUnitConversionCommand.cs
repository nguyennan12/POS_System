using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Products;

namespace POS.Application.UseCases.Products.Commands.UpdateUnitConversion;

public record UpdateUnitConversionCommand(
    Guid Id,
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice
) : ICommand<UnitConversionDto>, IRequirePermission
{
    public string RequiredPermission => "Products.Update";
}
