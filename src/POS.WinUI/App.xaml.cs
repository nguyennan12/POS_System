using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.WinUI.ApiClients;
using POS.WinUI.Extensions;
using POS.WinUI.ViewModels.Auth;
using POS.WinUI.ViewModels.Shell;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Shell;

namespace POS.WinUI;

public partial class App : Application
{
    private static IHost? _host;

    public static IHost Host => _host ?? throw new InvalidOperationException("Host is not initialized");

    public static IServiceProvider Services => Host.Services;

    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"Lỗi giao diện: {e.Exception.Message}\n{e.Exception.InnerException?.Message}", "POS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "POS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile("appsettings.Development.json", optional: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var apiBaseUrl = context.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";

                // Đăng ký HttpClient chung với base URL + NetworkStatusHandler + Polly retry
                services.AddHttpClient("PosApi", client =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Device-Id", Environment.MachineName);
                })
                .AddHttpMessageHandler<NetworkStatusHandler>()
                .AddStandardResilienceHandler();

                // Lấy HttpClient mặc định cho tất cả ApiClients
                services.AddTransient(sp =>
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("PosApi"));

                // Đăng ký toàn bộ WPF UI services, ViewModels, Views
                services.AddWinUIServices();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Host.Start();

        // Mặc định load LoginView vào MainWindow
        var mainViewModel = Host.Services.GetRequiredService<MainWindowViewModel>();
        var loginView = Host.Services.GetRequiredService<LoginView>();
        mainViewModel.CurrentView = loginView;

        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
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
