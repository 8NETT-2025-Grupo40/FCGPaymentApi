using System.Diagnostics;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Extensions.AWS.Trace;

namespace Fcg.Payment.Infrastructure.Messaging;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly string _queueUrl;

    public OutboxDispatcher(IServiceScopeFactory serviceScopeFactory, IAmazonSQS sqs, IConfiguration cfg, ILogger<OutboxDispatcher> logger)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._sqs = sqs;
        this._logger = logger;
        this._queueUrl = cfg["Sqs:PaymentConfirmedQueueUrl"] ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this._queueUrl))
        {
            this._logger.LogInformation("Sqs:PaymentConfirmedQueueUrl not configured. OutboxPublisher sleeping.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(this._queueUrl))
            {
                using IServiceScope scope = this._serviceScopeFactory.CreateScope();
                PaymentDbContext db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                // Caso não seja possível connectar no banco de dados, protege contra falhas em cascata.
                if (!await db.Database.CanConnectAsync(cancellationToken))
                {
                    this._logger.LogInformation("Cannot connect to database.");
                    continue;
                }

                var batch = await db.Outbox
                    .Where(x => x.SentAt == null)
                    .OrderBy(x => x.OccurredAt)
                    .Take(20)
                    .ToListAsync(cancellationToken);

                foreach (OutboxMessage msg in batch)
                {
                    try
                    {
                        // Escolha de agrupamento, por compra (purchaseId) ou por usuário (userId).
                        (string groupId, string dedupId) = BuildFifoMetadata(msg);

                        SendMessageRequest req = new()
                        {
                            QueueUrl = this._queueUrl,
                            MessageBody = msg.PayloadJson,
                            MessageAttributes = new()
                            {
                                ["Type"] = new() { DataType = "String", StringValue = msg.Type },
                                ["CorrelationId"] = new() { DataType = "String", StringValue = groupId },
                                ["ContentType"] = new() { DataType = "String", StringValue = "application/json" }
                            }
                        };

                        req.MessageGroupId = groupId;
                        req.MessageDeduplicationId = dedupId;

                        await this.SendMessageAsync(req, cancellationToken);

                        msg.SentAt = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.Attempts++;
                        this._logger.LogError($"Outbox publish fail: {ex.Message}");
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    private async Task SendMessageAsync(SendMessageRequest req, CancellationToken cancellationToken)
    {
        // Tenta usar o Activity atual (ex.: veio de uma request HTTP).
        // Se não houver (fora do pipeline HTTP ou sampler dropou),
        // cria um span do tipo Producer para representar o publish no SQS.
        using Activity? act = Activity.Current
                              ?? PaymentTelemetry.Source.StartActivity(
                                  "SQS Publish payment.confirmed", ActivityKind.Producer);

        // Se StartActivity retornou null (ninguém escutando a Source ou sampling desligado),
        // criamos um Activity "fallback" só para ter TraceId/SpanId válidos
        // e conseguir gerar o AWSTraceHeader mesmo assim.
        var activity = act ?? new Activity("SQS Publish (fallback)")
            .SetIdFormat(ActivityIdFormat.W3C);  // garante formato W3C (necessário p/ propagação)
        if (act is null)
        {
            activity.Start(); // sem Start() o Activity não gera IDs
        }

        // Injeta o cabeçalho do X-Ray na mensagem SQS (MessageSystemAttributes["AWSTraceHeader"]).
        // Isso é o que permite o X-Ray "ligar" Producer → SQS → Lambda no Trace Map.
        var hdr = InjectXRayHeader(activity, req);
        this._logger.LogInformation("AWSTraceHeader={Hdr}", hdr);

        // Publica na fila (cada retry deve criar um HttpRequestMessage novo — você já faz isso acima).
        await this._sqs.SendMessageAsync(req, cancellationToken);

        // Se usamos o fallback, precisamos parar manualmente.
        // Se "act" não é null, o using chama Dispose() e dá o Stop() para a gente.
        if (act is null)
        {
            activity.Stop();
        }
    }

    private static (string groupId, string dedupId) BuildFifoMetadata(OutboxMessage msg)
    {
        string groupId = "payments"; // fallback global
        using JsonDocument doc = JsonDocument.Parse(msg.PayloadJson);
        // Tente agrupar por compra
        if (doc.RootElement.TryGetProperty("purchaseId", out JsonElement pId) && pId.ValueKind is JsonValueKind.String)
        {
            groupId = $"purchase-{pId.GetString()}";
        }
        // Senão, por usuário
        else if (doc.RootElement.TryGetProperty("userId", out JsonElement uId) && uId.ValueKind is JsonValueKind.String)
        {
            groupId = $"user-{uId.GetString()}";
        }

        // Para dedupe, use o próprio Id do outbox (ou o purchaseId):
        string dedupId = msg.Id.ToString();
        return (groupId, dedupId);
    }

    private static string InjectXRayHeader(Activity act, SendMessageRequest req)
    {
        var propagator = new AWSXRayPropagator();
        req.MessageSystemAttributes ??= new();

        string? headerValue = null;
        propagator.Inject(
            new PropagationContext(act.Context, Baggage.Current),
            req,
            (carrier, key, value) =>
            {
                if (key.Equals("x-amzn-trace-id", StringComparison.OrdinalIgnoreCase))
                {
                    headerValue = value;
                    carrier.MessageSystemAttributes["AWSTraceHeader"] =
                        new MessageSystemAttributeValue { DataType = "String", StringValue = value };
                }
            });

        return headerValue ?? "<no-header>";
    }

}
public static class PaymentTelemetry
{
    public static readonly string FcgPaymentPublisherSourceName = "FCG.Payment";
    public static readonly ActivitySource Source = new(FcgPaymentPublisherSourceName);
}

