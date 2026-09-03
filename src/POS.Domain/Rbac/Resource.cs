using POS.Domain.Common;

namespace POS.Domain.Rbac;

public class Resource : BaseEntity
{
    public Resource() : base()
    {
    }

    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }

    public ICollection<Permission> Permissions { get; private set; } = new List<Permission>();
}
