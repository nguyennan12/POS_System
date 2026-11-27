using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Reports.Queries.GetDashboardSummary;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Reports;

public sealed class GetDashboardSummaryToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["days"];

    public string Name => AiToolNames.GetDashboardSummary;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadOptionalInt(arguments, "days", 7, 1, 90, out var days, out error))
            return error!;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetDashboardSummaryQuery(store.Value, days)
        {
            CurrentStoreOnly = true
        }, cancellationToken);
        return result.IsSuccess
            ? AiToolExecutionResult.Ok(result.Value!, 1)
            : AiToolErrors.FromDomainError(result.Error);
    }
}
