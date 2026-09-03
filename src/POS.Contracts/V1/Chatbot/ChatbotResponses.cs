namespace POS.Contracts.V1.Chatbot;

public record ChatSessionResponse(
    string SessionId,
    Guid StoreId,
    DateTimeOffset StartedAt
);

public record ChatMessageResponse(
    string Reply,
    DateTimeOffset CreatedAt
);

public record ChatMessageItemResponse(
    Guid Id,
    string Sender,
    string Content,
    DateTimeOffset CreatedAt
);

public record ChatHistoryResponse(
    string SessionId,
    IReadOnlyList<ChatMessageItemResponse> Messages
);
