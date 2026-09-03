using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Stores;

namespace POS.Domain.Chatbot;

public class ChatConversation : BaseEntity
{
    public ChatConversation() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    public string SessionId { get; private set; } = default!;
    public int MessageCount { get; private set; }
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }
}
