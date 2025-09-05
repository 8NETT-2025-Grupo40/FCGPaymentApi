using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Domain;

namespace Payment.Api.Infrastructure;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _sf;
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;

    public OutboxPublisher(IServiceScopeFactory sf, IAmazonSQS sqs, IConfiguration cfg)
    {
        _sf = sf; _sqs = sqs; _queueUrl = cfg["Sqs:PaymentConfirmedQueueUrl"] ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
            Console.WriteLine("Sqs:PaymentConfirmedQueueUrl not configured. OutboxPublisher sleeping.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(_queueUrl))
            {
                using var scope = _sf.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                var batch = await db.Outbox
                    .Where(x => x.SentAt == null)
                    .OrderBy(x => x.OccurredAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in batch)
                {
                    try
                    {
                        var req = new SendMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            MessageBody = msg.PayloadJson,
                            MessageAttributes = new()
                            {
                                ["Type"] = new() { DataType = "String", StringValue = msg.Type }
                            }
                        };

                        if (IsFifoQueue(_queueUrl))
                        {
                            // Escolha de agrupamento: por compra (purchaseId) ou por usuário (userId).
                            var (groupId, dedupId) = BuildFifoMetadata(msg);

                            req.MessageGroupId = groupId;                    // OBRIGATÓRIO em FIFO
                            req.MessageDeduplicationId = dedupId;            // Opcional se ContentBasedDedup=true
                        }

                        await _sqs.SendMessageAsync(req, stoppingToken);
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
        url?.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase) ?? false;

    private static (string groupId, string dedupId) BuildFifoMetadata(OutboxMessage msg)
    {
        string groupId = "payments"; // fallback global
        try
        {
            using var doc = JsonDocument.Parse(msg.PayloadJson);
            // Tente agrupar por compra (ordenação por compra)…
            if (doc.RootElement.TryGetProperty("purchaseId", out var pId) && pId.ValueKind is JsonValueKind.String)
                groupId = $"purchase-{pId.GetString()}";
            // …ou por usuário (ordenação por usuário)
            else if (doc.RootElement.TryGetProperty("userId", out var uId) && uId.ValueKind is JsonValueKind.String)
                groupId = $"user-{uId.GetString()}";
        }
        catch { /* se não der pra parsear, usa fallback */ }

        // Para dedupe, use o próprio Id do outbox (ou o purchaseId):
        string dedupId = msg.Id.ToString(); // se a fila tiver ContentBasedDedup=true, pode omitir
        return (groupId, dedupId);
    }
}
