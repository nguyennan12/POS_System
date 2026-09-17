using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Behaviors;

namespace POS.Application;

public static class DependencyInjection
{
    /// <summary>Registers application services, pipeline behaviors, and domain services.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        services.AddScoped<POS.Domain.Promotions.Services.IPromotionEngine, POS.Domain.Promotions.Services.PromotionEngine>();

        return services;
    }
}
