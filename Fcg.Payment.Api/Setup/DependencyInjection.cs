using Fcg.Payment.API.Middlewares;
using Fcg.Payment.Application.Payments;
using Fcg.Payment.Application.Ports;
using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Payments;
using Fcg.Payment.Infrastructure;
using Fcg.Payment.Infrastructure.Messaging;
using Fcg.Payment.Infrastructure.Repositories;
using Fcg.Payment.Infrastructure.Stores;

namespace Fcg.Payment.API.Setup;

public static class DependencyInjection
{
    public static void RegisterServices(this IServiceCollection services)
    {
        // Payment
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEventModelRepository, EventModelRepository>();
        services.AddScoped<IPaymentAppService, PaymentAppService>();

        // Infra
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IPaymentsWebhookHandler, PaymentsWebhookHandler>();
        services.AddHostedService<OutboxDispatcher>();

        // Common
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    public static void RegisterMiddlewares(this IServiceCollection services)
    {
        services.AddTransient<StructuredLogMiddleware>();
        services.AddTransient<GlobalErrorHandlingMiddleware>();
    }
}