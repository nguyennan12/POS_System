using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Chatbot;

public class FaqEntry : BaseEntity
{
    public FaqEntry() : base()
    {
    }

    public Guid? StoreId { get; private set; }
    public Store? Store { get; private set; }
    public string? Category { get; private set; }
    public string Question { get; private set; } = default!;
    public string Answer { get; private set; } = default!;
    public string? Keywords { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
