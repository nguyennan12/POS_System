using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using POS.Api.Notifications;
using POS.Api.Options;
using POS.Application.Abstractions.Notifications;

namespace POS.Api.Extensions;

public static class PaymentHubExtensions
{
    public const string CorsPolicy = "PaymentHub";

    public static IServiceCollection AddPaymentRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PaymentHubOptions>().Bind(configuration.GetSection(PaymentHubOptions.SectionName))
            .Validate(options => options.AllowedOrigins is not null && options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
                !uri.Host.Contains('*') && string.IsNullOrEmpty(uri.UserInfo) &&
                string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)),
                "PaymentHub:AllowedOrigins must contain exact HTTP(S) origins without paths or wildcards.")
            .ValidateOnStart();
        services.AddSignalR();
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<PaymentHubOptions>>((cors, options) =>
            cors.AddPolicy(CorsPolicy, policy => policy.WithOrigins(options.Value.AllowedOrigins)
                .WithMethods("GET", "POST").AllowAnyHeader().AllowCredentials()));
        services.Replace(ServiceDescriptor.Scoped<IRealtimeEventPublisher, SignalRRealtimeEventPublisher>());
        // TODO scale-out: configure a Redis backplane before deploying multiple API instances.
        return services;
    }
}
