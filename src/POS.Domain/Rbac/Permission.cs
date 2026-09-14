using POS.Domain.Common;
using POS.Domain.Rbac.Enums;

namespace POS.Domain.Rbac;

public class Permission : BaseEntity
{
  public Permission() : base()
  {
  }

  public Permission(Guid resourceId, Resource resource, PermissionAction action, string? description = null, Guid? id = null) : base(id)
  {
    ResourceId = resourceId;
    Resource = resource;
    Action = action;
    Description = description;
    Code = $"{resource.Code.ToLower()}:{action.ToString().ToLower()}";
  }

  public Guid ResourceId { get; private set; }
  public Resource Resource { get; private set; } = default!;

  public PermissionAction Action { get; private set; }
  public string Code { get; private set; } = default!;
  public string? Description { get; private set; }
}
