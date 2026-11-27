namespace POS.Application.Abstractions.AI;

public sealed record AiGenerationResponse(
    AiModelStatus Status,
    string? Text,
    IReadOnlyList<AiToolCall> ToolCalls,
    string? ProviderRequestId,
    int? InputTokens,
    int? OutputTokens)
{
    public static AiGenerationResponse Completed(
        string text,
        string? providerRequestId = null,
        int? inputTokens = null,
        int? outputTokens = null) =>
        new(AiModelStatus.Completed, text, Array.Empty<AiToolCall>(), providerRequestId, inputTokens, outputTokens);

    public static AiGenerationResponse RequiresAction(
        IReadOnlyList<AiToolCall> toolCalls,
        string? text = null,
        string? providerRequestId = null,
        int? inputTokens = null,
        int? outputTokens = null) =>
        new(AiModelStatus.RequiresAction, text, toolCalls, providerRequestId, inputTokens, outputTokens);
}
