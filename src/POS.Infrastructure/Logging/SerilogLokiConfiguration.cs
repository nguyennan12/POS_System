using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace POS.Infrastructure.Logging;

public static class SerilogLokiConfiguration
{

  /// Cấu hình Serilog ghi log ra Console + đẩy về Grafana Loki.
  /// lokiUrl truyền vào từ appsettings ("Serilog:LokiUrl"), ví dụ http://loki:3100 (trong docker network).
  /// </summary>
  public static LoggerConfiguration Configure(
      LoggerConfiguration loggerConfiguration,
      string? lokiUrl,
      string? lokiUser = null,
      string? lokiPassword = null,
      string appName = "pos-api",
      string env = "dev")
  {
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", appName)
        .Enrich.WithProperty("Environment", env)
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(lokiUrl))
    {
      LokiCredentials? credentials = null;
      if (!string.IsNullOrWhiteSpace(lokiUser) && !string.IsNullOrWhiteSpace(lokiPassword))
      {
        credentials = new LokiCredentials
        {
          Login = lokiUser,
          Password = lokiPassword
        };
      }

      loggerConfiguration.WriteTo.GrafanaLoki(
          lokiUrl,
          credentials: credentials,
          labels: new[]
          {
                    new LokiLabel { Key = "app", Value = appName },
                    new LokiLabel { Key = "env", Value = env }
          });
    }

    return loggerConfiguration;
  }
}
