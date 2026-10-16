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
    Code = $"{resource.Code.ToLowerInvariant()}:{ActionToCode(action)}";
  }

  public Guid ResourceId { get; private set; }
  public Resource Resource { get; private set; } = default!;

  public PermissionAction Action { get; private set; }
  public string Code { get; private set; } = default!;
  public string? Description { get; private set; }

  public static string ActionToCode(PermissionAction action) => action switch
  {
    PermissionAction.Read => "read",
    PermissionAction.Manage => "manage",
    PermissionAction.Create => "create",
    PermissionAction.Update => "update",
    PermissionAction.Delete => "delete",
    PermissionAction.CreateOwn => "create_own",
    PermissionAction.ReadOwn => "read_own",
    PermissionAction.CancelOwn => "cancel_own",
    PermissionAction.Cancel => "cancel",
    PermissionAction.ManageOwn => "manage_own",
    PermissionAction.ApplyOwn => "apply_own",
    PermissionAction.Complete => "complete",
    PermissionAction.Approve => "approve",
    PermissionAction.Dispose => "dispose",
    PermissionAction.Pay => "pay",
    PermissionAction.Import => "import",
    PermissionAction.PriceOverride => "price_override",
    PermissionAction.LoyaltyAdjust => "loyalty_adjust",
    PermissionAction.OverrideDiscount => "override_discount",
    PermissionAction.Refund => "refund",
    PermissionAction.AuditApprove => "audit_approve",
    PermissionAction.DashboardView => "dashboard_view",
    PermissionAction.RevenueView => "revenue_view",
    PermissionAction.InventoryView => "inventory_view",
    PermissionAction.ProfitView => "profit_view",
    PermissionAction.Export => "export",
    PermissionAction.ManageStore => "manage_store",
    PermissionAction.ManageGlobal => "manage_global",
    PermissionAction.LoginHistory => "login_history",
    _ => action.ToString().ToLowerInvariant()
  };
}
