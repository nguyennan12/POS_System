using System.Text.Json;
using Microsoft.Extensions.Logging;
using POS.Domain.Promotions;

namespace POS.Application.UseCases.Promotions.Services;

internal static class PromotionConditions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static T? Read<T>(Promotion promotion, ILogger logger) where T : class
    {
        if (string.IsNullOrWhiteSpace(promotion.ConditionsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(promotion.ConditionsJson, JsonOptions);
        }
        catch (JsonException)
        {
            logger.LogWarning("Invalid conditions JSON for promotion {PromotionId}", promotion.Id);
            return null;
        }
    }
}
