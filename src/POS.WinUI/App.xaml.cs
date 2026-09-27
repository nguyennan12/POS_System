using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.WinUI.ApiClients;
using POS.WinUI.Extensions;
using POS.WinUI.Services;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build host tại đây để exception xảy ra sau khi WPF App đã sẵn sàng,
        // giúp DispatcherUnhandledException có thể bắt và hiển thị cho user.
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

        _host.Start();

        // Dùng INavigationService để navigate đến LoginView — không hard-code nữa
        var nav = _host.Services.GetRequiredService<INavigationService>();
        nav.NavigateTo<LoginView>();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
