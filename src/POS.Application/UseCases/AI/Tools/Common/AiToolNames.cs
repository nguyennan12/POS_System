namespace POS.Application.UseCases.AI.Tools.Common;

/// <summary>
/// Hằng số tên chuẩn (snake_case) cho 22 công cụ AI chỉ đọc ở Release R1.
/// Cả Người 1 và Người 2 dùng trực tiếp các hằng số này trong <c>IAiToolHandler.Name</c> để tránh lệch tên với Registry.
/// </summary>
public static class AiToolNames
{
    // ---- Người 1: Tool 1–10 ----
    public const string SearchProducts = "search_products";
    public const string GetProductDetail = "get_product_detail";
    public const string GetInventory = "get_inventory";
    public const string GetStockAlerts = "get_stock_alerts";
    public const string ListOrders = "list_orders";
    public const string GetOrderDetail = "get_order_detail";
    public const string SearchInvoices = "search_invoices";
    public const string GetInvoiceDetail = "get_invoice_detail";
    public const string GetRevenueReport = "get_revenue_report";
    public const string GetTopSellingProducts = "get_top_selling_products";

    // ---- Người 2: Tool 11–22 ----
    public const string SearchCustomers = "search_customers";
    public const string GetCustomerDetail = "get_customer_detail";
    public const string GetCustomerLoyalty = "get_customer_loyalty";
    public const string GetCurrentShift = "get_current_shift";
    public const string ValidateVoucher = "validate_voucher";
    public const string GetDashboardSummary = "get_dashboard_summary";
    public const string GetSlowMovingProducts = "get_slow_moving_products";
    public const string GetStockHistory = "get_stock_history";
    public const string GetExpiringBatches = "get_expiring_batches";
    public const string ListActivePromotions = "list_active_promotions";
    public const string GetProductPromotions = "get_product_promotions";
    public const string PreviewOrderPromotions = "preview_order_promotions";
}
