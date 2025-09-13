using Fcg.Payment.Application;
using Fcg.Payment.Domain;
using Fcg.Payment.Domain.Common;
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
        services.AddScoped<IPaymentAppService, PaymentAppService>();

        // Infra
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IPaymentsWebhookHandler, PaymentsWebhookHandler>();
        services.AddHostedService<OutboxDispatcher>();

        // Common
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}