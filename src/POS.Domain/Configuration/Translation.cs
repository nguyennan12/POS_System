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
}
