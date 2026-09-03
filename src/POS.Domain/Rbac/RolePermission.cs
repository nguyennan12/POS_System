using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Domain.Rbac;

public class RolePermission : BaseEntity
{
    public RolePermission() : base()
    {
    }

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = default!;

    public Guid? GrantedBy { get; private set; }
    public Employee? GrantedByEmployee { get; private set; }
    public DateTime GrantedAt { get; private set; } = DateTime.UtcNow;
}
