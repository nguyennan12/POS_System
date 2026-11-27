using System.Text.Json;
using System.Text.Json.Serialization;

namespace POS.Infrastructure.AI.Groq;

internal sealed record GroqChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<GroqChatMessage> Messages,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens,
    [property: JsonPropertyName("stream")] bool Stream = false,
    [property: JsonPropertyName("tools"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<GroqToolDefinition>? Tools = null,
    [property: JsonPropertyName("tool_choice"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ToolChoice = null);

internal sealed record GroqChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("tool_calls"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<GroqToolCallPayload>? ToolCalls = null,
    [property: JsonPropertyName("tool_call_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ToolCallId = null,
    [property: JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Name = null);

internal sealed record GroqToolDefinition(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] GroqFunctionDefinition Function);

internal sealed record GroqFunctionDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] JsonElement Parameters);

internal sealed record GroqToolCallPayload(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("function")] GroqFunctionCallPayload? Function);

internal sealed record GroqFunctionCallPayload(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("arguments")] string? Arguments);

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

    [JsonPropertyName("tool_calls")]
    public List<GroqToolCallPayload>? ToolCalls { get; init; }
}

internal sealed class GroqUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? InputTokens { get; init; }

    [JsonPropertyName("completion_tokens")]
    public int? OutputTokens { get; init; }
}

internal sealed class GroqErrorEnvelope
{
    [JsonPropertyName("error")]
    public GroqErrorDetail? Error { get; init; }
}

internal sealed class GroqErrorDetail
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
