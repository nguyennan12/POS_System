using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.WinUI.Extensions;


namespace POS.WinUI;

internal static class Program
{
  [STAThread]
  static void Main()
  {
    ApplicationConfiguration.Initialize();

    var host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(config =>
        {
          config.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
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
              .AddStandardResilienceHandler(); // Polly retry + circuit breaker

          // Lấy HttpClient mặc định cho tất cả ApiClients
          services.AddTransient(sp =>
                  sp.GetRequiredService<IHttpClientFactory>().CreateClient("PosApi"));

          // Đăng ký tất cả WinUI services (Forms, Presenters, ApiClients, Services)
          services.AddWinUIServices();
        })
        .ConfigureLogging(logging =>
        {
          logging.ClearProviders();
          logging.AddConsole();
          logging.AddDebug();
        })
        .Build();

    // System.Windows.Forms.Application.Run(
    //     host.Services.GetRequiredService<frmLogin>());
    System.Windows.Forms.Application.Run();
  }
}
