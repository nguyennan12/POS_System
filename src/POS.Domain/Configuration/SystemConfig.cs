using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Configuration;

public class SystemConfig : BaseEntity
{
    public SystemConfig() : base()
    {
    }

    public Guid? StoreId { get; private set; }
    public Store? Store { get; private set; }
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
