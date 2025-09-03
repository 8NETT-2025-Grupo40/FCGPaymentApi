using Microsoft.EntityFrameworkCore;
using Payment.Api.Infrastructure;
using System.Text.Json;

namespace Payment.Api.Services;

public class PaymentService
{
    private readonly PaymentsDbContext _db;

    public PaymentService(PaymentsDbContext db) => _db = db;

    public async Task<Payment.Api.Contracts.CreatePaymentResponse> CreateAsync(
        Payment.Api.Contracts.CreatePaymentRequest req,
        string idemKey,
        Payment.Api.Psp.IPspClient psp,
        CancellationToken ct)
    {
        // 1) Idempotência de criação
        var existing = await _db.Idempotency.FindAsync([idemKey], ct);
        if (existing is not null)
            return JsonSerializer.Deserialize<Payment.Api.Contracts.CreatePaymentResponse>(existing.ResponseBody)!;

        // 2) Cria o pagamento (Pending)
        var payment = new Payment.Api.Domain.Payment
        {
            UserId = req.UserId,
            Amount = req.Items.Sum(i => i.UnitPrice * i.Quantity),
            Currency = req.Currency,
            Items = req.Items.Select(i => new Payment.Api.Domain.PaymentItem
            {
                GameId = i.GameId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        // 3) (Opcional) Gera um checkoutUrl "fake" via PSP adapter atual
        var (checkoutUrl, pspRef) = await psp.CreateCheckoutAsync(payment, ct);
        payment.PspReference = pspRef;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        // 4) AUTO-CAPTURE: confirma imediatamente e enfileira o evento na Outbox
        //    (somente se estiver habilitado em config)
        //if (_opt.Value.AutoCapture)
        //{
            payment.Status = Payment.Api.Domain.PaymentStatus.Captured;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            var evt = new
            {
                @event = "payment.confirmed",
                version = "1",
                occurredAt = DateTimeOffset.UtcNow,
                userId = payment.UserId,
                purchaseId = payment.PaymentId,
                items = payment.Items.Select(i => new
                {
                    gameId = i.GameId,
                    quantity = i.Quantity,
                    price = i.UnitPrice
                })
            };

            _db.Outbox.Add(new Payment.Api.Domain.OutboxMessage
            {
                Type = "payment.confirmed",
                PayloadJson = JsonSerializer.Serialize(evt)
            });

            await _db.SaveChangesAsync(ct);
        //}

        // 5) Resposta + registro da idempotência
        var resp = new Payment.Api.Contracts.CreatePaymentResponse(payment.PaymentId, checkoutUrl);

        _db.Idempotency.Add(new Payment.Api.Domain.IdempotencyKey
        {
            Key = idemKey,
            PayloadHash = "hash-do-payload", // opcional: calcule hash real do body
            ResponseBody = JsonSerializer.Serialize(resp)
        });
        await _db.SaveChangesAsync(ct);

        return resp;
    }
    public async Task HandleWebhookAsync(string rawBody, string signatureHeader, Payment.Api.Psp.IPspClient psp, CancellationToken ct)
    {
        if (!psp.TryValidateWebhookSignature(rawBody, signatureHeader))
            throw new UnauthorizedAccessException("Invalid signature");

        var (_, pspRef, status) = psp.ParseWebhook(rawBody);
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.PspReference == pspRef, ct)
                      ?? throw new InvalidOperationException("Payment not found");

        if (status is Payment.Api.Domain.PaymentStatus.Captured or Payment.Api.Domain.PaymentStatus.Authorized or Payment.Api.Domain.PaymentStatus.Failed or Payment.Api.Domain.PaymentStatus.Refunded)
        {
            payment.Status = status;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            if (status == Payment.Api.Domain.PaymentStatus.Captured)
            {
                var evt = new
                {
                    event_ = "payment.confirmed",
                    version = "1",
                    occurredAt = DateTimeOffset.UtcNow,
                    userId = payment.UserId,
                    purchaseId = payment.PaymentId,
                    items = payment.Items.Select(i => new { gameId = i.GameId, quantity = i.Quantity, price = i.UnitPrice })
                };
                _db.Outbox.Add(new Payment.Api.Domain.OutboxMessage
                {
                    Type = "payment.confirmed",
                    PayloadJson = JsonSerializer.Serialize(evt).Replace("event_", "event")
                });
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}
