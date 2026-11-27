using System.Text.Json.Serialization;

namespace POS.Application.UseCases.AI.Tools.Common;

public sealed record AiToolExecutionResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Data,
    [property: JsonPropertyName("resultCount")] int ResultCount,
    [property: JsonPropertyName("errorCode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ErrorCode = null,
    [property: JsonPropertyName("errorMessage"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ErrorMessage = null)
{
    public static AiToolExecutionResult Ok(object data, int resultCount) =>
        new(true, data, Math.Max(0, resultCount));

    public static AiToolExecutionResult EmptyList() =>
        new(true, Array.Empty<object>(), 0);

    public static AiToolExecutionResult Fail(string errorCode, string errorMessage) =>
        new(false, null, 0, errorCode, errorMessage);
}
