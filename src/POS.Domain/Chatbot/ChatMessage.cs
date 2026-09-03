using POS.Domain.Common;
using POS.Domain.Chatbot.Enums;

namespace POS.Domain.Chatbot;

public class ChatMessage : BaseEntity
{
    public ChatMessage() : base()
    {
    }

    public Guid ConversationId { get; private set; }
    public ChatConversation Conversation { get; private set; } = default!;

    public ChatSender Sender { get; private set; }
    public string Content { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
