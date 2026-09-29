using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.CreateSku;

public record CreateSkuCommand(
    Guid ProductId,
    string SkuCode,
    string Barcode,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    JsonElement? Attributes
) : ICommand<Guid>, IRequirePermission
{
    public string RequiredPermission => "Products.Create";
}
