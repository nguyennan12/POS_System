using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Promotions.Queries.PreviewOrderPromotions;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Promotions;

public sealed class PreviewOrderPromotionsToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["orderId"];

    public string Name => AiToolNames.PreviewOrderPromotions;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadRequiredGuid(arguments, "orderId", out var orderId, out error))
            return error!;

        var result = await sender.Send(new PreviewOrderPromotionsQuery(orderId), cancellationToken);
        return result.IsSuccess
            ? AiToolExecutionResult.Ok(result.Value!, 1)
            : AiToolErrors.FromDomainError(result.Error);
    }
}
