using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Commands.CreateProduct;

public record CreateSkuInfo(
    string SkuCode,
    string Barcode,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    JsonElement? Attributes
);

public record CreateProductCommand(
    string Name,
    Guid CategoryId,
    string BaseUnit,
    string? Description,
    string? Brand,
    string? ImageUrl,
    IReadOnlyList<CreateSkuInfo>? Skus
) : ICommand<Guid>, IRequirePermission
{
    public string RequiredPermission => "products:create";
}
