using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ApiClients;
using POS.WinUI.Services;
using POS.WinUI.ViewModels.Auth;
using POS.WinUI.ViewModels.Shell;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Shell;

namespace POS.WinUI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký toàn bộ services, ViewModels, Views của tầng WPF UI vào DI container.
    /// </summary>
    public static IServiceCollection AddWinUIServices(this IServiceCollection services)
    {
        // ── Services ──────────────────────────────────────────────
        services.AddSingleton<SessionService>();

        // ── ApiClients ────────────────────────────────────────────
        services.AddTransient<StoreApiClient>();

        // ── ViewModels ────────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();

        // ── Views ─────────────────────────────────────────────────
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginView>();

        return services;
    }
}
