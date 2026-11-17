namespace POS.Contracts.V1.Chatbot;

public sealed record AiChatTurnRequest(string Role, string Content);

public sealed record AskAiRequest(
    string Message,
    IReadOnlyList<AiChatTurnRequest?>? History = null);
