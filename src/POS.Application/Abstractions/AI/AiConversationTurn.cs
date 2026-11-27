namespace POS.Application.Abstractions.AI;

public sealed record AiConversationTurn(
    string Role,
    string? Content,
    IReadOnlyList<AiToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? Name = null)
{
    public static AiConversationTurn User(string content) =>
        new("user", content);

    public static AiConversationTurn Assistant(string content) =>
        new("assistant", content);

    public static AiConversationTurn AssistantToolCalls(string? content, IReadOnlyList<AiToolCall> toolCalls) =>
        new("assistant", content, ToolCalls: toolCalls);

    public static AiConversationTurn Tool(string toolCallId, string name, string resultJson) =>
        new("tool", resultJson, ToolCallId: toolCallId, Name: name);
}
