using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ApiClients;
using POS.WinUI.Forms.Stores;
using POS.WinUI.Presenters;
using POS.WinUI.Services;

namespace POS.WinUI.Extensions;

public static class ServiceCollectionExtensions
{
  /// Đăng ký toàn bộ services của tầng WinUI vào DI container.
  public static IServiceCollection AddWinUIServices(this IServiceCollection services)
  {
    // ── Services ──────────────────────────────────────────────
    services.AddSingleton<SessionService>();

    // ── ApiClients ────────────────────────────────────────────
    services.AddTransient<StoreApiClient>();

    // ── Presenters ────────────────────────────────────────────
    services.AddTransient<StorePresenter>();

    // ── Forms ─────────────────────────────────────────────────
    services.AddTransient<frmStoreList>();
    services.AddTransient<frmStoreEdit>();


    return services;
  }
}
