using POS.Api.Exceptions;
using POS.Api.Extensions;
using POS.Application;
using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Persistence;
using Prometheus;
using Serilog;

// ---------- 1. Bootstrap Logger ----------
var currentEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var envLabel = currentEnv.Equals("Development", StringComparison.OrdinalIgnoreCase) ? "dev" : currentEnv.ToLower();
var lokiUrl = Environment.GetEnvironmentVariable("Serilog__LokiUrl");

Log.Logger = SerilogLokiConfiguration
    .Configure(new LoggerConfiguration(), lokiUrl, env: envLabel)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------- 2. Serilog Host Integration ----------
    var appEnv = builder.Environment.IsDevelopment() ? "dev" : builder.Environment.EnvironmentName.ToLower();
    builder.Host.UseSerilog((context, services, configuration) =>
        SerilogLokiConfiguration.Configure(
            configuration,
            context.Configuration["Serilog:LokiUrl"],
            env: appEnv));

    // ---------- 3. Exceptions & Problem Details ----------
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ---------- 4. Dependency Injection (Application + Infrastructure) ----------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ---------- 5. Authentication & Authorization ----------
    builder.Services.AddPosAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<POS.Application.Abstractions.Auth.ICurrentUser, POS.Api.Auth.CurrentUser>();

    // ---------- 6. Controllers & OpenAPI ----------
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddOpenApiDocumentation();

    // ---------- 7. Health Checks ----------
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgresql")
        .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis");

    var app = builder.Build();

    // ---------- 8. Middleware Pipeline ----------
    app.UseExceptionHandler();
    app.UsePosSerilogRequestLogging();
    app.UseRequestResponseLogging();

    app.MapOpenApiDocumentation();

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Prometheus Metrics & Health Checks
    app.UseHttpMetrics();
    app.MapMetrics();
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/db");

    // ---------- 9. Auto-migrate & Seed Data (Dev Only) ----------
    if (app.Environment.IsDevelopment())
    {
        await app.ApplyMigrationsAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "POS.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
