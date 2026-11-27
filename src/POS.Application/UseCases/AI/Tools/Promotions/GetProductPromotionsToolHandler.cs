using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Promotions.Queries.GetApplicablePromotionsForSku;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Promotions;

public sealed class GetProductPromotionsToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["skuId", "quantity"];

    public string Name => AiToolNames.GetProductPromotions;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadRequiredGuid(arguments, "skuId", out var skuId, out error) ||
            !TryReadOptionalDecimal(arguments, "quantity", 1m, 0m, decimal.MaxValue, out var quantity, out error))
            return error!;

        var result = await sender.Send(new GetApplicablePromotionsForSkuQuery(skuId, quantity), cancellationToken);
        return result.IsSuccess
            ? AiToolExecutionResult.Ok(result.Value!, result.Value!.Promotions.Count)
            : AiToolErrors.FromDomainError(result.Error);
    }
}
