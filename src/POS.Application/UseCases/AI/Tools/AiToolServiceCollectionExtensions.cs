using Microsoft.Extensions.DependencyInjection;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;

namespace POS.Application.UseCases.AI.Tools;

/// <summary>
/// Điểm đăng ký DI tập trung cho hệ thống AI Tools để tránh xung đột trên <c>DependencyInjection.cs</c>.
/// - Người 1 đăng ký các handler Tool 1–10 trong <see cref="AddPerson1CoreAiToolHandlers"/>.
/// - Người 2 đăng ký các handler Tool 11–22 trong <see cref="AddPerson2ExtendedAiToolHandlers"/>.
/// </summary>
public static class AiToolServiceCollectionExtensions
{
    public static IServiceCollection AddAiTooling(this IServiceCollection services)
    {
        services.AddSingleton<IAiToolRegistry, AiToolRegistry>();
        services.AddScoped<IAiToolExecutor, AiToolExecutor>();

        services.AddPerson1CoreAiToolHandlers();
        services.AddPerson2ExtendedAiToolHandlers();

        return services;
    }

    private static IServiceCollection AddPerson1CoreAiToolHandlers(this IServiceCollection services)
    {
        // Người 1 đăng ký Tool 1–10 tại đây (AI-P1-07 → AI-P1-10):
        // services.AddScoped<IAiToolHandler, SearchProductsToolHandler>();
        return services;
    }

    private static IServiceCollection AddPerson2ExtendedAiToolHandlers(this IServiceCollection services)
    {
        // Người 2 đăng ký Tool 11–22 tại đây (AI-P2-05 → AI-P2-09):
        // services.AddScoped<IAiToolHandler, SearchCustomersToolHandler>();
        return services;
    }
}
