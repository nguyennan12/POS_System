namespace POS.Contracts.V1.Chatbot;

public record CreateChatSessionRequest(
    Guid StoreId
);

public record SendChatMessageRequest(
    string SessionId,
    string Message
);
