using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Products.Queries.GetProductById;

namespace POS.Application.UseCases.AI.Tools.Products;

public sealed record ProductUnitConversionToolDto(
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice);

public sealed record ProductSkuDetailToolDto(
    Guid SkuId,
    string SkuCode,
    string Barcode,
    JsonElement? Attributes,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    decimal QtyOnHand,
    IReadOnlyList<ProductUnitConversionToolDto> UnitConversions);

public sealed record ProductDetailToolDto(
    Guid ProductId,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    string? Brand,
    string BaseUnit,
    string Status,
    IReadOnlyList<ProductSkuDetailToolDto> Skus);

public sealed class GetProductDetailToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "productId" };

    public string Name => AiToolNames.GetProductDetail;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredGuid(arguments, "productId", out var productId, out error))
        {
            return error!;
        }

        var result = await sender.Send(new GetProductByIdQuery(productId), cancellationToken);
        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var product = result.Value;
        var skus = product.Skus
            .Select(sku => new ProductSkuDetailToolDto(
                SkuId: sku.Id,
                SkuCode: sku.SkuCode,
                Barcode: sku.Barcode,
                Attributes: sku.Attributes,
                SellPrice: sku.SellPrice,
                TaxRate: sku.TaxRate,
                IsActive: sku.IsActive,
                QtyOnHand: sku.QtyOnHand,
                UnitConversions: sku.UnitConversions
                    .Select(uc => new ProductUnitConversionToolDto(
                        uc.UnitName,
                        uc.ConversionFactor,
                        uc.SellPrice))
                    .ToArray()))
            .ToArray();

        var safeDetail = new ProductDetailToolDto(
            ProductId: product.Id,
            CategoryId: product.CategoryId,
            CategoryName: product.CategoryName,
            Name: product.Name,
            Description: product.Description,
            Brand: product.Brand,
            BaseUnit: product.BaseUnit,
            Status: product.Status,
            Skus: skus);

        return AiToolExecutionResult.Ok(safeDetail, 1);
    }
}
