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

  // ---------- Controllers + Swagger ----------
  builder.Services.AddControllers();
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen(options =>
  {
    options.SwaggerDoc("v1", new() { Title = "POS API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
      Name = "Authorization",
      Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
      Scheme = "bearer",
      BearerFormat = "JWT",
      In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new()
      {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
      });
  });

  // ---------- Health checks: SQL Server + Redis (dùng cho Docker healthcheck & /health/db) ----------
  builder.Services.AddHealthChecks()
      .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "sqlserver")
      .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis");

  var app = builder.Build();

  // ---------- Middleware pipeline ----------
  app.UseExceptionHandler();
  app.UseSerilogRequestLogging();

  if (app.Environment.IsDevelopment())
  {
    app.UseSwagger();
    app.UseSwaggerUI();
  }

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
