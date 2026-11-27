using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Reports.Queries.GetTopSellingReport;

namespace POS.Application.UseCases.AI.Tools.Reports;

public sealed record TopSellingProductToolItemDto(
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal QuantitySold,
    decimal TotalRevenue);

public sealed class GetTopSellingProductsToolHandler(
    ISender sender,
    IStoreAccessPolicy storeAccessPolicy) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "from", "to", "limit" };

    public string Name => AiToolNames.GetTopSellingProducts;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredDateRangeUtc(arguments, "from", "to", out var fromUtc, out var toUtc, out error) ||
            !AiToolArgumentReader.TryReadOptionalInt(arguments, "limit", defaultValue: 10, min: 1, max: 20, out var limit, out error))
        {
            return error!;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return AiToolErrors.FromDomainError(storeContext.Error);
        }

        var result = await sender.Send(
            new GetTopSellingReportQuery(
                From: fromUtc,
                To: toUtc,
                Limit: limit,
                StoreId: storeContext.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var safeItems = result.Value
            .Take(limit)
            .Select(item => new TopSellingProductToolItemDto(
                SkuId: item.SkuId,
                SkuCode: item.SkuCode,
                ProductName: item.ProductName,
                QuantitySold: item.QuantitySold,
                TotalRevenue: item.TotalRevenue))
            .ToArray();

        return AiToolExecutionResult.Ok(safeItems, safeItems.Length);
    }
}
