using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Auth;
using POS.Infrastructure.Auth;
using POS.Infrastructure.Cache;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using StackExchange.Redis;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- PostgreSQL ----
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // ---- Redis ----
        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<ICacheService, RedisCacheService>();

        // ---- Repositories / UnitOfWork / Migration ----
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IEmployeeStoreAccessRepository, EmployeeStoreAccessRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMigrationService, MigrationService>();

        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // ---- Auth ----
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPinLookupHasher, PinLookupHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPinLoginRateLimiter, PinLoginRateLimiter>();

        // ---- Configuration ----
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        return services;
    }
}