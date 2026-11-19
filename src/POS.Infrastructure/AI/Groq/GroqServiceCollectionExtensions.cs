using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.AI;

namespace POS.Infrastructure.AI.Groq;

public static class GroqServiceCollectionExtensions
{
    public static IServiceCollection AddGroqAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<GroqOptions>, GroqOptionsValidator>();
        services.AddOptions<GroqOptions>()
            .Bind(configuration.GetSection(GroqOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<IAiModelClient, GroqClient>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<GroqOptions>>().Value;
            var baseUrl = settings.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        return services;
    }
}
