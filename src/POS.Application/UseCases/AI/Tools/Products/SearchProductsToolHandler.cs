using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Products.Queries.GetPosCatalog;

namespace POS.Application.UseCases.AI.Tools.Products;

public sealed record SearchProductToolItemDto(
    Guid ProductId,
    Guid SkuId,
    string ProductName,
    string SkuCode,
    string Barcode,
    decimal SellPrice,
    decimal QtyOnHand,
    string BaseUnit,
    string CategoryName);

public sealed class SearchProductsToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "keyword", "limit" };

    public string Name => AiToolNames.SearchProducts;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredString(arguments, "keyword", minLength: 1, maxLength: 100, out var keyword, out error) ||
            !AiToolArgumentReader.TryReadOptionalInt(arguments, "limit", defaultValue: 10, min: 1, max: 20, out var limit, out error))
        {
            return error!;
        }

        var result = await sender.Send(
            new GetPosCatalogQuery(
                CategoryId: null,
                Search: keyword,
                PageNumber: 1,
                PageSize: limit,
                StoreId: null),
            cancellationToken);

        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var safeItems = result.Value.Items
            .Select(item => new SearchProductToolItemDto(
                ProductId: item.ProductId,
                SkuId: item.SkuId,
                ProductName: item.ProductName,
                SkuCode: item.SkuCode,
                Barcode: item.Barcode,
                SellPrice: item.SellPrice,
                QtyOnHand: item.QtyOnHand,
                BaseUnit: item.BaseUnit,
                CategoryName: item.CategoryName))
            .ToArray();

        return AiToolExecutionResult.Ok(safeItems, safeItems.Length);
    }
}
