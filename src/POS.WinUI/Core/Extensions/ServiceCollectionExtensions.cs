using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Auth;
using POS.WinUI.ViewModels.Cashier;
using POS.WinUI.ViewModels.Customers;
using POS.WinUI.ViewModels.Dashboard;
using POS.WinUI.ViewModels.Employees;
using POS.WinUI.ViewModels.Inventory;
using POS.WinUI.ViewModels.Orders;
using POS.WinUI.ViewModels.Products;
using POS.WinUI.ViewModels.Promotions;
using POS.WinUI.ViewModels.Reports;
using POS.WinUI.ViewModels.Settings;
using POS.WinUI.ViewModels.Shell;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Cashier;
using POS.WinUI.Views.Customers;
using POS.WinUI.Views.Dashboard;
using POS.WinUI.Views.Employees;
using POS.WinUI.Views.Inventory;
using POS.WinUI.Views.Orders;
using POS.WinUI.Views.Products;
using POS.WinUI.Views.Promotions;
using POS.WinUI.Views.Reports;
using POS.WinUI.Views.Settings;
using POS.WinUI.Views.Shell;

namespace POS.WinUI.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWinUIServices(this IServiceCollection services)
    {
        // ── Core Services ─────────────────────────────────────────
        services.AddSingleton<SessionService>();
        services.AddSingleton<NetworkStatusService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ── ApiClients & Handlers ──────────────────────────────────
        services.AddTransient<NetworkStatusHandler>();
        services.AddTransient<StoreApiClient>();
        services.AddTransient<AuthApiClient>();
        services.AddTransient<ShiftApiClient>();

        // ── ViewModels ────────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<ManagementMainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<PosCashierMainViewModel>();

        // ── 9 Feature ViewModels ──────────────────────────────────
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<PromotionsViewModel>();
        services.AddTransient<EmployeesViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();

        // ── Views ─────────────────────────────────────────────────
        services.AddSingleton<MainWindow>();
        services.AddTransient<ManagementMainView>();
        services.AddTransient<LoginView>();
        services.AddTransient<PosCashierMainView>();
        services.AddTransient<DashboardView>();
        services.AddTransient<OrdersView>();
        services.AddTransient<ProductsView>();
        services.AddTransient<InventoryView>();
        services.AddTransient<CustomersView>();
        services.AddTransient<PromotionsView>();
        services.AddTransient<EmployeesView>();
        services.AddTransient<ReportsView>();
        services.AddTransient<SettingsView>();

        return services;
    }
}
