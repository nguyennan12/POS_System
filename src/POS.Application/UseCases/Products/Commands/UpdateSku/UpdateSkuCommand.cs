using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.UpdateSku;

public record UpdateSkuCommand(
    Guid Id,
    string SkuCode,
    string Barcode,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    JsonElement? Attributes
) : ICommand<Guid>, IRequirePermission
{
    public string RequiredPermission => "products:update";
}
