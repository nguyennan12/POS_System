using System.Text.Json.Serialization;

namespace POS.Infrastructure.AI.Gemini;

internal sealed record GeminiInteractionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] IReadOnlyList<GeminiInputStep> Input,
    [property: JsonPropertyName("system_instruction")] string SystemInstruction,
    [property: JsonPropertyName("generation_config")] GeminiGenerationConfig GenerationConfig);

internal sealed record GeminiInputStep(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("content")] IReadOnlyList<GeminiTextContent> Content);

internal sealed record GeminiTextContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens);

internal sealed class GeminiInteractionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("steps")]
    public List<GeminiOutputStep>? Steps { get; init; }

    [JsonPropertyName("usage")]
    public GeminiUsage? Usage { get; init; }
}

internal sealed class GeminiOutputStep
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("content")]
    public List<GeminiTextContent>? Content { get; init; }
}

internal sealed class GeminiUsage
{
    [JsonPropertyName("total_input_tokens")]
    public int? InputTokens { get; init; }

    [JsonPropertyName("total_output_tokens")]
    public int? OutputTokens { get; init; }
}
