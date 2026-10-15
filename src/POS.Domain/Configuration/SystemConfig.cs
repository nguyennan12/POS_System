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

    public static SystemConfig Create(Guid? storeId, string key, string value) =>
        new() { StoreId = storeId, Key = key, Value = value, UpdatedAt = DateTime.UtcNow };

    public void SetValue(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
