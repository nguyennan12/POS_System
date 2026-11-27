namespace POS.Application.Abstractions.AI;

public sealed record AiGenerationRequest(
    string SystemInstruction,
    IReadOnlyList<AiConversationTurn> Messages,
    IReadOnlyList<AiToolDefinition>? Tools = null);
