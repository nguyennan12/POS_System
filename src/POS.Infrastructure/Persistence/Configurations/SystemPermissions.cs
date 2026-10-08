using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public record ResourceDefinition(
    string Code,
    string Description,
    (PermissionAction Action, string Description)[] Actions
);

public static class SystemPermissions
{
    public static readonly ResourceDefinition[] Resources =
    [
        // 1. Stores
        new(ResourceNames.Stores, "Store Management",
        [
            (PermissionAction.Read, "View store details and list"),
            (PermissionAction.Manage, "Create, update, manage store status and assign manager")
        ]),

        // 2. Roles & Permissions
        new(ResourceNames.Roles, "Role & Permission Management",
        [
            (PermissionAction.Manage, "Manage custom roles and assign permissions")
        ]),

        // 3. Employees
        new(ResourceNames.Employees, "Employee Management",
        [
            (PermissionAction.Read, "View employee profiles and list"),
            (PermissionAction.Manage, "Create, update, lock/unlock employees, reset PIN/password"),
            (PermissionAction.LoginHistory, "View employee login history and audit logs")
        ]),

        // 4. Categories
        new(ResourceNames.Categories, "Category Management",
        [
            (PermissionAction.Read, "View product categories"),
            (PermissionAction.Manage, "Create, update, and delete product categories")
        ]),

        // 5. Products
        new(ResourceNames.Products, "Product Catalog Management",
        [
            (PermissionAction.Read, "View product catalog, pricing, and details"),
            (PermissionAction.Manage, "Create, update, and deactivate products"),
            (PermissionAction.Import, "Bulk import products from Excel")
        ]),

        // 6. SKUs
        new(ResourceNames.Skus, "SKU & Pricing Management",
        [
            (PermissionAction.Read, "Scan barcode, view SKU details, price and stock availability"),
            (PermissionAction.Manage, "Create, update, deactivate SKUs and unit conversions"),
            (PermissionAction.PriceOverride, "Configure time-based and customer-group price lists")
        ]),

        // 7. Inventory
        new(ResourceNames.Inventory, "Inventory & Stock Management",
        [
            (PermissionAction.Read, "View current stock levels and expiry alerts"),
            (PermissionAction.Dispose, "Dispose damaged or expired stock")
        ]),

        // 8. Stock In Vouchers
        new(ResourceNames.StockInVouchers, "Stock In Voucher Management",
        [
            (PermissionAction.Read, "View stock in vouchers and receiving history"),
            (PermissionAction.Manage, "Create, edit, and cancel draft stock in vouchers"),
            (PermissionAction.Complete, "Complete stock in voucher, increase stock and calculate cost")
        ]),

        // 9. Stock Takes
        new(ResourceNames.StockTakes, "Stock Take & Count Management",
        [
            (PermissionAction.Manage, "Create stock take sessions and input counted quantities"),
            (PermissionAction.Approve, "Approve stock take and adjust inventory balance")
        ]),

        // 10. Suppliers
        new(ResourceNames.Suppliers, "Supplier Management",
        [
            (PermissionAction.Read, "View supplier details and payables"),
            (PermissionAction.Manage, "Create, update, and deactivate suppliers"),
            (PermissionAction.Pay, "Record supplier debt payments")
        ]),

        // 11. Shifts
        new(ResourceNames.Shifts, "Shift & Cash Reconciliation",
        [
            (PermissionAction.ManageOwn, "Open, view, and close own shift with cash count"),
            (PermissionAction.Read, "View store shift history and cash summaries"),
            (PermissionAction.AuditApprove, "Approve cash discrepancy audit reports")
        ]),

        // 12. Orders
        new(ResourceNames.Orders, "Order & Sales Management",
        [
            (PermissionAction.CreateOwn, "Create sales orders and add items in open shift"),
            (PermissionAction.ReadOwn, "View own created orders in shift"),
            (PermissionAction.Read, "View store-wide and chain-wide sales orders"),
            (PermissionAction.CancelOwn, "Cancel own unpaid draft orders"),
            (PermissionAction.Cancel, "Cancel any order with manager authorization"),
            (PermissionAction.OverrideDiscount, "Apply manual discounts overriding standard rules"),
            (PermissionAction.Refund, "Process refunds and item returns for paid orders")
        ]),

        // 13. Payments
        new(ResourceNames.Payments, "Payment Processing",
        [
            (PermissionAction.CreateOwn, "Create payment transactions (Cash, QR, Card, Points) for own orders"),
            (PermissionAction.Read, "View payment transaction status and details")
        ]),

        // 14. Invoices
        new(ResourceNames.Invoices, "Invoice & Receipt Management",
        [
            (PermissionAction.Read, "View, print, and export sales receipts / invoices")
        ]),

        // 15. Customers
        new(ResourceNames.Customers, "Customer & CRM Management",
        [
            (PermissionAction.Read, "Lookup and view customer profiles"),
            (PermissionAction.Create, "Register new customer profiles at POS"),
            (PermissionAction.Manage, "Update customer details and status"),
            (PermissionAction.LoyaltyAdjust, "Manually adjust customer loyalty points with reason")
        ]),

        // 16. Member Tiers
        new(ResourceNames.MemberTiers, "Member Tier Configuration",
        [
            (PermissionAction.Read, "View membership tiers"),
            (PermissionAction.Manage, "Configure membership tier spending thresholds and point rates")
        ]),

        // 17. Promotions
        new(ResourceNames.Promotions, "Promotions Management",
        [
            (PermissionAction.Read, "View active promotional campaigns"),
            (PermissionAction.Manage, "Create, update, and close promotional campaigns")
        ]),

        // 18. Vouchers
        new(ResourceNames.Vouchers, "Voucher Management",
        [
            (PermissionAction.Read, "View and validate vouchers"),
            (PermissionAction.ApplyOwn, "Apply voucher code to cart at POS"),
            (PermissionAction.Manage, "Issue, update, and revoke voucher codes")
        ]),

        // 19. Reports
        new(ResourceNames.Reports, "Reports & Analytics",
        [
            (PermissionAction.DashboardView, "View real-time dashboard analytics"),
            (PermissionAction.RevenueView, "View detailed revenue and sales breakdown"),
            (PermissionAction.InventoryView, "View stock status, low stock, and slow moving reports"),
            (PermissionAction.ProfitView, "View gross profit reports (Revenue vs Cost)"),
            (PermissionAction.Export, "Export reports to Excel and PDF formats")
        ]),

        // 20. Config
        new(ResourceNames.Config, "System & Store Configuration",
        [
            (PermissionAction.Read, "Read system settings and i18n translations"),
            (PermissionAction.ManageStore, "Configure store-specific settings and receipts"),
            (PermissionAction.ManageGlobal, "Configure global chain-wide settings and policies")
        ]),

        // 21. Audit Logs
        new(ResourceNames.AuditLogs, "Audit Log Management",
        [
            (PermissionAction.Read, "View system security audit trail and logs")
        ])
    ];
}
