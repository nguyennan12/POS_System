using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Domain.Rbac;

public class RolePermission : BaseEntity
{
  public RolePermission() : base()
  {
  }

  public RolePermission(Guid roleId, Role role, Guid permissionId, Permission permission, Guid? grantedBy = null, Employee? grantedByEmployee = null, Guid? id = null) : base(id)
  {
    RoleId = roleId;
    Role = role;
    PermissionId = permissionId;
    Permission = permission;
    GrantedBy = grantedBy;
    GrantedByEmployee = grantedByEmployee;
    GrantedAt = DateTime.UtcNow;
  }

  public Guid RoleId { get; private set; }
  public Role Role { get; private set; } = default!;

  public Guid PermissionId { get; private set; }
  public Permission Permission { get; private set; } = default!;

  public Guid? GrantedBy { get; private set; }
  public Employee? GrantedByEmployee { get; private set; }
  public DateTime GrantedAt { get; private set; } = DateTime.UtcNow;
}
