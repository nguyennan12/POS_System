namespace POS.Application.Abstractions.AI;

public sealed record AiGenerationRequest(
    string SystemInstruction,
    IReadOnlyList<AiConversationTurn> Messages);
