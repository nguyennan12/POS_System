using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                var apiBaseUrl = context.Configuration["ApiSettings:BaseUrl"]
                                ?? "https://localhost:7000/";

                // Đăng ký HttpClient chung với base URL + Polly retry
                services.AddHttpClient("PosApi", client =>
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        await Host.StartAsync();

        // Mặc định load LoginView vào MainWindow
        var mainViewModel = Host.Services.GetRequiredService<MainWindowViewModel>();
        var loginView = Host.Services.GetRequiredService<LoginView>();
        mainViewModel.CurrentView = loginView;

        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
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
