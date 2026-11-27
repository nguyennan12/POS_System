using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockTransactions;
using POS.Domain.Inventory.Enums;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Inventory;

public sealed class GetStockHistoryToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["skuId", "type", "limit"];

    public string Name => AiToolNames.GetStockHistory;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadRequiredGuid(arguments, "skuId", out var skuId, out error) ||
            !TryReadOptionalEnum<StockTransactionType>(arguments, "type", out _, out var type, out error) ||
            !TryReadOptionalInt(arguments, "limit", 20, 1, 100, out var limit, out error))
            return error!;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetStockTransactionsQuery(skuId, type, 1, limit), cancellationToken);
        if (result.IsFailure)
            return AiToolErrors.FromDomainError(result.Error);

        var transactions = result.Value!.Items.Select(transaction => new
        {
            transaction.Id,
            transaction.SkuId,
            transaction.SkuCode,
            transaction.Type,
            transaction.Qty,
            transaction.OrderId,
            transaction.StockInVoucherId,
            transaction.CreatedAt
        }).ToArray();
        return AiToolExecutionResult.Ok(transactions, transactions.Length);
    }
}
