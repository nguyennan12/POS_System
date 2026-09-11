using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using POS.Api.Exceptions;
using POS.Api.Extensions;
using POS.Application;
using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Persistence;
using Serilog;


Log.Logger = SerilogLokiConfiguration
    .Configure(new LoggerConfiguration(), Environment.GetEnvironmentVariable("Serilog__LokiUrl"))
    .CreateLogger();

try
{
  var builder = WebApplication.CreateBuilder(args);
  // ---------- Exceptions ----------
  builder.Services.AddProblemDetails();
  builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

  // ---------- Logging Loki ----------
  builder.Host.UseSerilog((context, services, configuration) =>
      SerilogLokiConfiguration.Configure(configuration, context.Configuration["Serilog:LokiUrl"]));

  // ---------- DI: Application + Infrastructure (SQL Server + Redis wiring nằm trong đây) ----------
  builder.Services.AddApplication();
  builder.Services.AddInfrastructure(builder.Configuration);

  // ---------- JWT Auth ----------
  var jwtSecret = builder.Configuration["Jwt:Secret"]
      ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

  builder.Services.AddAuthentication(options =>
  {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  }).AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = builder.Configuration["Jwt:Issuer"],
      ValidAudience = builder.Configuration["Jwt:Audience"],
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
  });

  builder.Services.AddAuthorization();

  // ---------- Controllers + OpenAPI / Scalar ----------
  builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
      });
  builder.Services.AddOpenApiDocumentation();

  // ---------- Health checks: SQL Server + Redis (dùng cho Docker healthcheck & /health/db) ----------
  builder.Services.AddHealthChecks()
      .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "sqlserver")
      .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis");

  var app = builder.Build();

  // ---------- Middleware pipeline ----------
  app.UseExceptionHandler();
  app.UseSerilogRequestLogging();

  app.MapOpenApiDocumentation();

  app.UseHttpsRedirection();
  app.UseAuthentication();
  app.UseAuthorization();

  app.MapControllers();
  app.MapHealthChecks("/health/db");

  // ---------- Auto-migrate & Seed data khi khởi động ----------
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
