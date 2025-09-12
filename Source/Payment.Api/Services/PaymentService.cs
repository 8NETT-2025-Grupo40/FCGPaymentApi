using System.Text.Json;
using Fcg.Payment.API.Contracts;
using Fcg.Payment.API.Domain;
using Fcg.Payment.API.Infrastructure;
using Fcg.Payment.API.Psp;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payment.API.Services;

public class PaymentService
{
    private readonly PaymentsDbContext _db;

    public PaymentService(PaymentsDbContext db) => this._db = db;

    public async Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest req,
        string idemKey,
        IPspClient psp,
        CancellationToken ct)
    {
        // Idempotência de criação
        var existing = await this._db.Idempotency.FindAsync([idemKey], ct);
        if (existing is not null)
            return JsonSerializer.Deserialize<CreatePaymentResponse>(existing.ResponseBody)!;

        // Cria o pagamento (Pending)
        var payment = new Domain.Payment
        {
            UserId = req.UserId,
            Amount = req.Items.Sum(i => i.UnitPrice * i.Quantity),
            Currency = req.Currency,
            Items = req.Items.Select(i => new PaymentItem
            {
                GameId = i.GameId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        this._db.Payments.Add(payment);
        await this._db.SaveChangesAsync(ct);

        // Gera um checkoutUrl "fake" via PSP adapter atual
        var (checkoutUrl, pspRef) = await psp.CreateCheckoutAsync(payment, ct);
        payment.PspReference = pspRef;
        payment.CreatedAt = DateTimeOffset.UtcNow;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await this._db.SaveChangesAsync(ct);

        // AUTO-CAPTURE: confirma imediatamente e enfileira o evento na Outbox
        //    (somente se estiver habilitado em config)
        //if (_opt.Value.AutoCapture)
        //{
            //payment.Status = Payment.Api.Domain.PaymentStatus.Captured;
            //payment.UpdatedAt = DateTimeOffset.UtcNow;

            //var evt = new
            //{
            //    @event = "payment.confirmed",
            //    version = "1",
            //    occurredAt = DateTimeOffset.UtcNow,
            //    userId = payment.UserId,
            //    purchaseId = payment.PaymentId,
            //    items = payment.Items.Select(i => new
            //    {
            //        gameId = i.GameId,
            //        quantity = i.Quantity,
            //        price = i.UnitPrice
            //    })
            //};

            //_db.Outbox.Add(new Payment.Api.Domain.OutboxMessage
            //{
            //    Type = "payment.confirmed",
            //    PayloadJson = JsonSerializer.Serialize(evt)
            //});

            //await _db.SaveChangesAsync(ct);
        //}

        // Resposta + registro da idempotência
        var resp = new CreatePaymentResponse(payment.PaymentId, checkoutUrl);

        this._db.Idempotency.Add(new IdempotencyKey
        {
            Key = idemKey,
            PayloadHash = "hash-do-payload", // opcional: calcule hash real do body
            ResponseBody = JsonSerializer.Serialize(resp)
        });
        await this._db.SaveChangesAsync(ct);

        return resp;
    }
    public async Task HandleWebhookAsync(string rawBody, string signatureHeader, IPspClient psp, CancellationToken ct)
    {
        if (!psp.TryValidateWebhookSignature(rawBody, signatureHeader))
            throw new UnauthorizedAccessException("Invalid signature");

        var (_, pspRef, status) = psp.ParseWebhook(rawBody);
        var payment = await this._db.Payments.FirstOrDefaultAsync(x => x.PspReference == pspRef, ct)
                      ?? throw new InvalidOperationException("Payment not found");

        if (status is PaymentStatus.Captured or PaymentStatus.Authorized or PaymentStatus.Failed or PaymentStatus.Refunded)
        {
            payment.Status = status;
            payment.UpdatedAt = DateTimeOffset.UtcNow;

            if (status == PaymentStatus.Captured)
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

                this._db.Outbox.Add(new OutboxMessage
                {
                    Type = "payment.confirmed",
                    PayloadJson = JsonSerializer.Serialize(evt).Replace("event_", "event")
                });
            }
            await this._db.SaveChangesAsync(ct);
        }
    }
}
