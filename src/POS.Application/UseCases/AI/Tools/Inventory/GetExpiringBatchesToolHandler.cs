using System.Globalization;
using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockBatches;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Inventory;

public sealed class GetExpiringBatchesToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["skuId", "expiryBefore", "limit"];

    public string Name => AiToolNames.GetExpiringBatches;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadOptionalGuid(arguments, "skuId", out var skuId, out error) ||
            !TryReadRequiredString(arguments, "expiryBefore", 10, 10, out var dateText, out error) ||
            !TryReadOptionalInt(arguments, "limit", 20, 1, 100, out var limit, out error))
            return error!;

        if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var expiryBefore))
            return AiToolErrors.InvalidArgument("Hạn sử dụng phải theo định dạng ngày YYYY-MM-DD.");

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetStockBatchesQuery(skuId, expiryBefore, PageSize: limit)
        {
            ScopedStoreId = store.Value
        }, cancellationToken);
        return result.IsSuccess
            ? AiToolExecutionResult.Ok(result.Value!.Items, result.Value.Items.Count)
            : AiToolErrors.FromDomainError(result.Error);
    }
}
