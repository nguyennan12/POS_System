using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ApiClients;
using POS.WinUI.Services;
using POS.WinUI.ViewModels.Auth;
using POS.WinUI.ViewModels.Products;
using POS.WinUI.ViewModels.Shell;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Products;
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
        services.AddTransient<CategoryApiClient>();
        services.AddTransient<ProductApiClient>();
        // HealthApiClient đã bị [Obsolete] — không đăng ký để tránh dùng nhầm

        // ── ViewModels ────────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductEditViewModel>();

        // ── Views ─────────────────────────────────────────────────
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginView>();
        services.AddTransient<ProductListView>();

        return services;
    }
}
