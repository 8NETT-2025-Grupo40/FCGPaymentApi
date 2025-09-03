using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;

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
        {
            Console.WriteLine("Sqs:PaymentConfirmedQueueUrl not configured. OutboxPublisher sleeping.");
        }

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
                        await _sqs.SendMessageAsync(new SendMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            MessageBody = msg.PayloadJson,
                            MessageAttributes = new()
                            {
                                ["Type"] = new() { DataType = "String", StringValue = msg.Type }
                            }
                        }, stoppingToken);

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
}
