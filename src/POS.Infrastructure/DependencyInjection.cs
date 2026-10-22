using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Import;
using POS.Infrastructure.Auth;
using POS.Infrastructure.Cache;
using POS.Infrastructure.Import;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using POS.Application.Abstractions.Payments;
using POS.Application.Abstractions.Notifications;
using POS.Application.UseCases.Payments.DTOs;
using POS.Infrastructure.Payments;
using POS.Infrastructure.Notifications;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- PostgreSQL ----
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

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
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IMemberTierRepository, MemberTierRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPosRegisterRepository, PosRegisterRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMigrationService, MigrationService>();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISkuRepository, SkuRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IStockEntryRepository, StockEntryRepository>();
        services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
        services.AddScoped<IStockBatchRepository, StockBatchRepository>();
        services.AddScoped<IStockInVoucherRepository, StockInVoucherRepository>();
        services.AddScoped<IStockTakeRepository, StockTakeRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IVoucherUsageRepository, VoucherUsageRepository>();
        services.AddScoped<IUnitConversionRepository, UnitConversionRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
        services.AddScoped<ITranslationRepository, TranslationRepository>();

        // ---- Excel Services (Reusable Import / Export) ----
        services.AddScoped<POS.Application.Abstractions.Excel.IExcelReader, POS.Infrastructure.Excel.ClosedXmlExcelReader>();
        services.AddScoped<POS.Application.Abstractions.Excel.IExcelExporter, POS.Infrastructure.Excel.ClosedXmlExcelExporter>();
        services.AddScoped<IExcelImportParser, ClosedXmlExcelImportParser>();

        // ---- Reports & Export Job Store ----
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddSingleton<POS.Application.Abstractions.Reports.IReportExportJobStore, POS.Infrastructure.Reports.ReportExportJobStore>();


        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // ---- Auth ----
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPinLookupHasher, PinLookupHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPinLoginRateLimiter, PinLoginRateLimiter>();

        // ---- Configuration ----
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // TODO T58: replace options source with SystemConfigs; keep secrets outside source control.
        services.Configure<MomoOptions>(configuration.GetSection("Payments:MoMo"));
        services.Configure<VietQrOptions>(configuration.GetSection("Payments:VietQR"));
        services.AddOptions<QrPaymentOptions>().Bind(configuration.GetSection("Payments:Qr"))
            .Validate(o => o.LifetimeMinutes is >= 1 and <= 120).ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<QrPaymentOptions>>().Value);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IWebhookSignatureVerifier, MomoWebhookSignatureVerifier>();
        services.AddScoped<IWebhookSignatureVerifier, VietQrWebhookSignatureVerifier>();
        services.AddHttpClient<MoMoPaymentAdapter>((sp, client) =>
        {
            var endpoint = new Uri(sp.GetRequiredService<IOptions<MomoOptions>>().Value.ApiBaseUrl);
            if (endpoint.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("MoMo requires HTTPS.");
            client.BaseAddress = endpoint;
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt)));
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<MoMoPaymentAdapter>());
        services.AddScoped<IPaymentGateway, VietQRAdapter>();
        services.AddScoped<IRealtimeEventPublisher, NoOpRealtimeEventPublisher>();

        return services;
    }
}
