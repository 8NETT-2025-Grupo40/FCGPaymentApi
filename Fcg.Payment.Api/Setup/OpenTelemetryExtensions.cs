using Fcg.Payment.Infrastructure.Messaging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Extensions.AWS.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Fcg.Payment.API.Setup
{
    public static class OpenTelemetryExtensions
    {
        public static IServiceCollection SetupOpenTelemetry(this IServiceCollection services)
        {
            // Propagação de contexto:
            // - AwsXRayPropagator: entende o header X-Amzn-Trace-Id (API Gateway, etc.)
            // - TraceContext/Baggage: padrão W3C (HttpClient, serviços .NET)
            Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([
                new AWSXRayPropagator(),
                new TraceContextPropagator(),
                new BaggagePropagator()
            ]));

            services.AddOpenTelemetry()
                // Identidade do serviço nos traces (aparece no X-Ray/Service Map)
                .ConfigureResource(r => r.AddService("fcg-payment-api"))
                .WithTracing(t => t
                    // Spans automáticos para requisições ASP.NET Core
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        // Enriquece span com http.route para CloudWatch Application Signals
                        o.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            var endpoint = httpRequest.HttpContext.GetEndpoint();
                            if (endpoint is Microsoft.AspNetCore.Routing.RouteEndpoint routeEndpoint)
                            {
                                activity.SetTag("http.route", routeEndpoint.RoutePattern.RawText);
                            }
                        };
                    })
                    // Propaga trace nas chamadas entre serviços
                    .AddHttpClientInstrumentation()
                    // Spans de queries/persistência
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(PaymentTelemetry.FcgPaymentPublisherSourceName)
                    // Exporta pro collector (ADOT/X-Ray)
                    .AddOtlpExporter());

            return services;
        }
    }
}

