using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

                        await this._sqs.SendMessageAsync(req, cancellationToken);
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
}
