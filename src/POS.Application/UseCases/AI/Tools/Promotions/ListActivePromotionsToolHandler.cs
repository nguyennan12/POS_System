using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Promotions.Queries.GetActivePromotionDetails;
using POS.Domain.Promotions.Enums;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Promotions;

public sealed class ListActivePromotionsToolHandler(ISender sender) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["type", "limit"];

    public string Name => AiToolNames.ListActivePromotions;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadOptionalEnum<PromotionType>(arguments, "type", out var type, out _, out error) ||
            !TryReadOptionalInt(arguments, "limit", 20, 1, 20, out var limit, out error))
            return error!;

        var result = await sender.Send(new GetActivePromotionDetailsQuery(type, limit), cancellationToken);
        return result.IsSuccess
            ? AiToolExecutionResult.Ok(result.Value!, result.Value!.Count)
            : AiToolErrors.FromDomainError(result.Error);
    }
}
