using POS.Domain.Common;

namespace POS.Domain.Configuration;

public class Translation : BaseEntity
{
    public Translation() : base()
    {
    }

    public string LanguageCode { get; private set; } = default!;
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static Translation Create(string languageCode, string key, string value) =>
        new() { LanguageCode = languageCode, Key = key, Value = value, UpdatedAt = DateTime.UtcNow };

    public void SetValue(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
