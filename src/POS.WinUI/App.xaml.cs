using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Extensions;
using POS.WinUI.Core.Services;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Shell;

namespace POS.WinUI;

public partial class App : Application
{
  private static IHost? _host;

  public static IHost Host => _host ?? throw new InvalidOperationException("Host chưa được khởi tạo.");
  public static IServiceProvider Services => Host.Services;

  public App()
  {
    // Bắt exception không xử lý ở cấp UI thread
    DispatcherUnhandledException += (s, e) =>
    {
      MessageBox.Show(
              $"Lỗi giao diện: {e.Exception.Message}\n{e.Exception.InnerException?.Message}",
              "POS Error", MessageBoxButton.OK, MessageBoxImage.Error);
      e.Handled = true;
    };

    // Bắt exception không xử lý ở thread khác
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    {
      if (e.ExceptionObject is Exception ex)
        MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "POS Error", MessageBoxButton.OK, MessageBoxImage.Error);
    };
  }

  protected override async void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    // 1. Hiển thị SplashScreen ngay lập tức (0ms)
    var splash = new SplashScreenWindow();
    splash.Show();

    // 2. Chạy tác vụ khởi động ngầm không chặn UI Thread + Anti-flicker threshold (600ms)
    await Task.Run(async () =>
    {
      var minDelayTask = Task.Delay(1000);

      splash.UpdateStatus("Đang nạp cấu hình hệ thống...");

      _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
              .ConfigureAppConfiguration((_, config) =>
              {
              config.SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("appsettings.Development.json", optional: true)
                        .AddEnvironmentVariables();
            })
              .ConfigureServices((context, services) =>
              {
              var apiBaseUrl = context.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";

                // HttpClient chính cho toàn bộ ApiClients: có resilience + NetworkStatusHandler
              services.AddHttpClient("PosApi", client =>
                  {
                  client.BaseAddress = new Uri(apiBaseUrl);
                  client.Timeout = TimeSpan.FromSeconds(30);
                  client.DefaultRequestHeaders.TryAddWithoutValidation("X-Device-Id", Environment.MachineName);
                })
                  .AddHttpMessageHandler<NetworkStatusHandler>()
                  .AddStandardResilienceHandler();

                // HttpClient mặc định resolve cho tất cả ApiClients
              services.AddTransient(sp =>
                      sp.GetRequiredService<IHttpClientFactory>().CreateClient("PosApi"));

                // Toàn bộ WPF UI services, ViewModels, Views
              services.AddWinUIServices();
            })
              .ConfigureLogging(logging =>
              {
              logging.ClearProviders();
              logging.AddConsole();
              logging.AddDebug();
            })
              .Build();

      await _host.StartAsync();

      splash.UpdateStatus("Chuẩn bị quầy thu ngân...");
      await minDelayTask;
    });

    // 3. Chuẩn bị MainWindow & Navigate đến LoginView
    var nav = _host!.Services.GetRequiredService<INavigationService>();
    nav.NavigateTo<LoginView>();

    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
    MainWindow = mainWindow;

    // 4. Mở tự động màn hình phụ cho khách hàng (Customer Facing Display)
    var cfdService = _host.Services.GetRequiredService<ICfdDisplayService>();
    cfdService.OpenCfd();

    // 5. Mờ dần Splash Screen và mở MainWindow êm dịu
    await splash.FadeOutAndCloseAsync(200);
    mainWindow.Show();
    mainWindow.Activate();
  }

  protected override async void OnExit(ExitEventArgs e)
  {
    if (_host != null)
    {
      var cfdService = _host.Services.GetService<ICfdDisplayService>();
      cfdService?.CloseCfd();

      await _host.StopAsync();
      _host.Dispose();
    }

    base.OnExit(e);
  }
}
