using POS.Domain.Common;
using POS.Domain.Rbac.Enums;

namespace POS.Domain.Rbac;

public class Permission : BaseEntity
{
    public Permission() : base()
    {
    }

    public Guid ResourceId { get; private set; }
    public Resource Resource { get; private set; } = default!;

    public PermissionAction Action { get; private set; }
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
}
