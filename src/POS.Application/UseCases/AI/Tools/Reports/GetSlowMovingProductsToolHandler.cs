using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Reports.Queries.GetSlowMovingReport;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Reports;

public sealed class GetSlowMovingProductsToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["days", "limit"];

    public string Name => AiToolNames.GetSlowMovingProducts;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadOptionalInt(arguments, "days", 30, 1, 365, out var days, out error) ||
            !TryReadOptionalInt(arguments, "limit", 20, 1, 50, out var limit, out error))
            return error!;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetSlowMovingReportQuery(days, store.Value)
        {
            CurrentStoreOnly = true
        }, cancellationToken);
        if (result.IsFailure)
            return AiToolErrors.FromDomainError(result.Error);

        var products = result.Value!.Take(limit).ToArray();
        return AiToolExecutionResult.Ok(products, products.Length);
    }
}
