using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ApiClients;
using POS.WinUI.Services;
using POS.WinUI.ViewModels.Auth;
using POS.WinUI.ViewModels.Management;
using POS.WinUI.ViewModels.Shell;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Management;
using POS.WinUI.Views.Shell;

namespace POS.WinUI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký toàn bộ services, ViewModels, Views của tầng WPF UI vào DI container.
    /// </summary>
    public static IServiceCollection AddWinUIServices(this IServiceCollection services)
    {
        // ── Core Services ─────────────────────────────────────────
        services.AddSingleton<SessionService>();
        services.AddSingleton<NetworkStatusService>();

        // INavigationService là Singleton vì giữ ref đến MainWindowViewModel
        services.AddSingleton<INavigationService, NavigationService>();

        // ── ApiClients & Handlers ──────────────────────────────────
        services.AddTransient<NetworkStatusHandler>();
        services.AddTransient<StoreApiClient>();
        services.AddTransient<AuthApiClient>();
        services.AddTransient<ShiftApiClient>();
        // HealthApiClient đã bị [Obsolete] — không đăng ký để tránh dùng nhầm

        // ── ViewModels ────────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ManagementMainViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Cashier.PosCashierMainViewModel>();

        // ── Management Tab / Feature ViewModels ───────────────────
        services.AddTransient<POS.WinUI.ViewModels.Dashboard.DashboardViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.OrdersTabViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.ProductsTabViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.InventoryTabViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.CustomersTabViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.EmployeesTabViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.ReportsTabViewModel>();
        services.AddTransient<POS.WinUI.ViewModels.Management.Tabs.SettingsTabViewModel>();

        // ── Views ─────────────────────────────────────────────────
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginView>();
        services.AddTransient<ManagementMainView>();
        services.AddTransient<POS.WinUI.Views.Cashier.PosCashierMainView>();

        return services;
    }
}
