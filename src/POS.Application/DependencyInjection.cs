using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Abstractions.Auth;
using POS.Application.Common.Behaviors;
using POS.Application.UseCases.AI.Tools;
using POS.Application.UseCases.Auth;
using POS.Application.UseCases.Promotions.Queries;
using POS.Application.UseCases.Promotions.Services;

namespace POS.Application;

public static class DependencyInjection
{
  ///  Registers application services, pipeline behaviors, and domain services.</summary>
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    // ---- Access Policy ----
    services.AddScoped<IStoreAccessPolicy, StoreAccessPolicy>();
    services.AddScoped<IPaymentSubscriptionAccessPolicy, POS.Application.UseCases.Payments.PaymentSubscriptionAccessPolicy>();
    services.AddScoped<POS.Application.UseCases.Payments.QrWebhookProcessor>();

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
    services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

    services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

    services.AddScoped<POS.Domain.Promotions.Services.IPromotionEngine, POS.Domain.Promotions.Services.PromotionEngine>();
    services.AddScoped<PromotionDetailsReader>();
    services.AddScoped<IPromotionPreviewService, PromotionPreviewService>();
    services.AddScoped<POS.Application.UseCases.Orders.Services.ICartCalculationService, POS.Application.UseCases.Orders.Services.CartCalculationService>();
    services.AddScoped<POS.Application.UseCases.Orders.Services.ICheckoutGuardService, POS.Application.UseCases.Orders.Services.CheckoutGuardService>();
    services.AddAiTooling();

    // ---- Payment Strategies & Factory ----
    services.AddScoped<POS.Application.Abstractions.Payments.IPaymentStrategy, POS.Application.UseCases.Payments.Strategies.CashPaymentStrategy>();
    services.AddScoped<POS.Application.Abstractions.Payments.IPaymentStrategy, POS.Application.UseCases.Payments.Strategies.MoMoPaymentStrategy>();
    services.AddScoped<POS.Application.Abstractions.Payments.IPaymentStrategy, POS.Application.UseCases.Payments.Strategies.VietQrPaymentStrategy>();
    services.AddScoped<POS.Application.Abstractions.Payments.IPaymentStrategy, POS.Application.UseCases.Payments.Strategies.CardPaymentStrategy>();
    services.AddScoped<POS.Application.Abstractions.Payments.IPaymentStrategy, POS.Application.UseCases.Payments.Strategies.PointsPaymentStrategy>();
    services.AddScoped<POS.Application.Abstractions.Payments.IPaymentStrategyFactory, POS.Application.UseCases.Payments.PaymentStrategyFactory>();

    return services;
  }
}
