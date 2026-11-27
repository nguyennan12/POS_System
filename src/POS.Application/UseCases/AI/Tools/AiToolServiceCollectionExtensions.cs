using Microsoft.Extensions.DependencyInjection;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.AI.Tools.Customers;
using POS.Application.UseCases.AI.Tools.Inventory;
using POS.Application.UseCases.AI.Tools.Invoices;
using POS.Application.UseCases.AI.Tools.Orders;
using POS.Application.UseCases.AI.Tools.Products;
using POS.Application.UseCases.AI.Tools.Reports;
using POS.Application.UseCases.AI.Tools.Shifts;
using POS.Application.UseCases.Orders.Access;

namespace POS.Application.UseCases.AI.Tools;

/// <summary>
/// Điểm đăng ký DI tập trung cho hệ thống AI Tools để tránh xung đột trên <c>DependencyInjection.cs</c>.
/// - Người 1 đăng ký các handler Tool 1–10 trong <see cref="AddPerson1CoreAiToolHandlers"/>.
/// - Người 2 đăng ký các handler Tool 11–22 trong <see cref="AddPerson2ExtendedAiToolHandlers"/>.
/// </summary>
public static class AiToolServiceCollectionExtensions
{
    public static IServiceCollection AddAiTooling(this IServiceCollection services)
    {
        services.AddSingleton<IAiToolRegistry, AiToolRegistry>();
        services.AddScoped<AiToolExecutor>();
        services.AddScoped<IAiToolExecutor, AuditedAiToolExecutor>();
        services.AddScoped<IOrderReadAccessPolicy, OrderReadAccessPolicy>();

        services.AddPerson1CoreAiToolHandlers();
        services.AddPerson2ExtendedAiToolHandlers();

        return services;
    }

    private static IServiceCollection AddPerson1CoreAiToolHandlers(this IServiceCollection services)
    {
        // Tool 1–2: Sản phẩm
        services.AddScoped<IAiToolHandler, SearchProductsToolHandler>();
        services.AddScoped<IAiToolHandler, GetProductDetailToolHandler>();

        // Tool 3–4: Tồn kho & Cảnh báo
        services.AddScoped<IAiToolHandler, GetInventoryToolHandler>();
        services.AddScoped<IAiToolHandler, GetStockAlertsToolHandler>();

        // Tool 5–6: Đơn hàng (hỗ trợ orders:read và orders:read_own)
        services.AddScoped<IAiToolHandler, ListOrdersToolHandler>();
        services.AddScoped<IAiToolHandler, GetOrderDetailToolHandler>();

        // Tool 7–8: Hóa đơn
        services.AddScoped<IAiToolHandler, SearchInvoicesToolHandler>();
        services.AddScoped<IAiToolHandler, GetInvoiceDetailToolHandler>();

        // Tool 9–10: Báo cáo doanh thu & Bán chạy
        services.AddScoped<IAiToolHandler, GetRevenueReportToolHandler>();
        services.AddScoped<IAiToolHandler, GetTopSellingProductsToolHandler>();

        return services;
    }

    private static IServiceCollection AddPerson2ExtendedAiToolHandlers(this IServiceCollection services)
    {
        services.AddScoped<IAiToolHandler, SearchCustomersToolHandler>();
        services.AddScoped<IAiToolHandler, GetCustomerDetailToolHandler>();
        services.AddScoped<IAiToolHandler, GetCustomerLoyaltyToolHandler>();
        services.AddScoped<IAiToolHandler, GetCurrentShiftToolHandler>();
        services.AddScoped<IAiToolHandler, GetDashboardSummaryToolHandler>();
        services.AddScoped<IAiToolHandler, GetSlowMovingProductsToolHandler>();
        services.AddScoped<IAiToolHandler, GetStockHistoryToolHandler>();
        services.AddScoped<IAiToolHandler, GetExpiringBatchesToolHandler>();
        return services;
    }
}
