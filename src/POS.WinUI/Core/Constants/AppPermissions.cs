namespace POS.WinUI.Core.Constants;

public static class AppPermissions
{
    public static class Stores
    {
        public const string Read = "stores:read";
        public const string Manage = "stores:manage";
    }

    public static class Roles
    {
        public const string Manage = "roles:manage";
    }

    public static class Employees
    {
        public const string Read = "employees:read";
        public const string Manage = "employees:manage";
        public const string LoginHistory = "employees:login_history";
    }

    public static class Categories
    {
        public const string Read = "categories:read";
        public const string Manage = "categories:manage";
    }

    public static class Products
    {
        public const string Read = "products:read";
        public const string Manage = "products:manage";
        public const string Import = "products:import";
    }

    public static class Skus
    {
        public const string Read = "skus:read";
        public const string Manage = "skus:manage";
        public const string PriceOverride = "skus:price_override";
    }

    public static class Inventory
    {
        public const string Read = "inventory:read";
        public const string Dispose = "inventory:dispose";
    }

    public static class StockInVouchers
    {
        public const string Read = "stock_in_vouchers:read";
        public const string Manage = "stock_in_vouchers:manage";
        public const string Complete = "stock_in_vouchers:complete";
    }

    public static class StockTakes
    {
        public const string Manage = "stock_takes:manage";
        public const string Approve = "stock_takes:approve";
    }

    public static class Suppliers
    {
        public const string Read = "suppliers:read";
        public const string Manage = "suppliers:manage";
        public const string Pay = "suppliers:pay";
    }

    public static class Shifts
    {
        public const string ManageOwn = "shifts:manage_own";
        public const string Read = "shifts:read";
        public const string AuditApprove = "shifts:audit_approve";
    }

    public static class Orders
    {
        public const string CreateOwn = "orders:create_own";
        public const string ReadOwn = "orders:read_own";
        public const string Read = "orders:read";
        public const string CancelOwn = "orders:cancel_own";
        public const string Cancel = "orders:cancel";
        public const string OverrideDiscount = "orders:override_discount";
        public const string Refund = "orders:refund";
    }

    public static class Payments
    {
        public const string CreateOwn = "payments:create_own";
        public const string Read = "payments:read";
    }

    public static class Invoices
    {
        public const string Read = "invoices:read";
    }

    public static class Customers
    {
        public const string Read = "customers:read";
        public const string Create = "customers:create";
        public const string Manage = "customers:manage";
        public const string LoyaltyAdjust = "customers:loyalty_adjust";
    }

    public static class MemberTiers
    {
        public const string Read = "member_tiers:read";
        public const string Manage = "member_tiers:manage";
    }

    public static class Promotions
    {
        public const string Read = "promotions:read";
        public const string Manage = "promotions:manage";
    }

    public static class Vouchers
    {
        public const string Read = "vouchers:read";
        public const string ApplyOwn = "vouchers:apply_own";
        public const string Manage = "vouchers:manage";
    }

    public static class Reports
    {
        public const string DashboardView = "reports:dashboard_view";
        public const string RevenueView = "reports:revenue_view";
        public const string InventoryView = "reports:inventory_view";
        public const string ProfitView = "reports:profit_view";
        public const string Export = "reports:export";
    }

    public static class Config
    {
        public const string Read = "config:read";
        public const string ManageStore = "config:manage_store";
        public const string ManageGlobal = "config:manage_global";
    }

    public static class AuditLogs
    {
        public const string Read = "audit_logs:read";
    }
}
