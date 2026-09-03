using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace POS.Infrastructure.Logging;

public static class SerilogLokiConfiguration
{
    /// <summary>
    /// Cấu hình Serilog ghi log ra Console + đẩy về Grafana Loki.
    /// lokiUrl truyền vào từ appsettings ("Serilog:LokiUrl"), ví dụ http://loki:3100 (trong docker network).
    /// </summary>
    public static LoggerConfiguration Configure(LoggerConfiguration loggerConfiguration, string? lokiUrl, string appName = "pos-api")
    {
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", appName)
            .WriteTo.Console();

        if (!string.IsNullOrWhiteSpace(lokiUrl))
        {
            loggerConfiguration.WriteTo.GrafanaLoki(
                lokiUrl,
                labels: new[] { new LokiLabel { Key = "app", Value = appName } });
        }

        return loggerConfiguration;
    }
}
