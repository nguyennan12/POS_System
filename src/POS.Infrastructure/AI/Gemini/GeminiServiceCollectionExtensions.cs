using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.AI;

namespace POS.Infrastructure.AI.Gemini;

public static class GeminiServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<GeminiOptions>, GeminiOptionsValidator>();
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<IAiModelClient, GeminiClient>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            var baseUrl = settings.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        return services;
    }
}
