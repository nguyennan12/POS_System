using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Stores;

namespace POS.Domain.Auditing;

public class AuditLog : BaseEntity
{
    public AuditLog() : base()
    {
    }

    public Guid? StoreId { get; private set; }
    public Store? Store { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string? Description { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
