using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Fcg.Payment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fcg.Payment.Infrastructure.Messaging;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;

    public OutboxDispatcher(IServiceScopeFactory serviceScopeFactory, IAmazonSQS sqs, IConfiguration cfg)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._sqs = sqs;
        this._queueUrl = cfg["Sqs:PaymentConfirmedQueueUrl"] ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(this._queueUrl))
        {
            Console.WriteLine("Sqs:PaymentConfirmedQueueUrl not configured. OutboxPublisher sleeping.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(this._queueUrl))
            {
                using IServiceScope scope = this._serviceScopeFactory.CreateScope();
                PaymentDbContext db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                var batch = await db.Outbox
                    .Where(x => x.SentAt == null)
                    .OrderBy(x => x.OccurredAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (OutboxMessage msg in batch)
                {
                    try
                    {
                        SendMessageRequest req = new()
                        {
                            QueueUrl = this._queueUrl,
                            MessageBody = msg.PayloadJson,
                            MessageAttributes = new()
                            {
                                ["Type"] = new() { DataType = "String", StringValue = msg.Type }
                            }
                        };

                        if (IsFifoQueue(this._queueUrl))
                        {
                            // Escolha de agrupamento, por compra (purchaseId) ou por usuário (userId).
                            (string groupId, string dedupId) = BuildFifoMetadata(msg);

                            req.MessageGroupId = groupId;
                            req.MessageDeduplicationId = dedupId;
                        }

                        await this._sqs.SendMessageAsync(req, stoppingToken);
                        msg.SentAt = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.Attempts++;
                        Console.WriteLine($"Outbox publish fail: {ex.Message}");
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private static bool IsFifoQueue(string url) =>
        url.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);

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
        string dedupId = msg.Id.ToString(); // se a fila tiver ContentBasedDedup=true, pode omitir
        return (groupId, dedupId);
    }
}
