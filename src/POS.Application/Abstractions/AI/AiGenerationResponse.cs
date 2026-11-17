namespace POS.Application.Abstractions.AI;

public sealed record AiGenerationResponse(
    string Text,
    string? ProviderRequestId,
    int? InputTokens,
    int? OutputTokens);
