using POS.Domain.Common;

namespace POS.Domain.Employees;

public class RefreshToken : BaseEntity
{
    public RefreshToken() : base()
    {
    }

    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = default!;

    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
