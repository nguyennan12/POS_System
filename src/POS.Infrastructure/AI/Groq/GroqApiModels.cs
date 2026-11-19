using System.Text.Json.Serialization;

namespace POS.Infrastructure.AI.Groq;

internal sealed record GroqChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<GroqChatMessage> Messages,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens,
    [property: JsonPropertyName("stream")] bool Stream = false);

internal sealed record GroqChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed class GroqChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("choices")]
    public List<GroqChatChoice>? Choices { get; init; }

    [JsonPropertyName("usage")]
    public GroqUsage? Usage { get; init; }
}

internal sealed class GroqChatChoice
{
    [JsonPropertyName("message")]
    public GroqResponseMessage? Message { get; init; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}

internal sealed class GroqResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}

internal sealed class GroqUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? InputTokens { get; init; }

    [JsonPropertyName("completion_tokens")]
    public int? OutputTokens { get; init; }
}
