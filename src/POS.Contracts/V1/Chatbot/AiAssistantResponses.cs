namespace POS.Contracts.V1.Chatbot;

public sealed record AskAiResponse(
    string Reply,
    DateTimeOffset CreatedAt);
