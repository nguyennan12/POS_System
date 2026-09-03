using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Stores;

namespace POS.Domain.Rbac;

public class Role : BaseEntity
{
    public Role() : base()
    {
    }

    public Role(string name, bool isSystemRole = false, Guid? storeId = null, string? description = null, Guid? id = null)
        : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsSystemRole = isSystemRole;
        StoreId = storeId;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid? StoreId { get; private set; }
    public Store? Store { get; private set; }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<Employee> Employees { get; private set; } = new List<Employee>();
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
}
