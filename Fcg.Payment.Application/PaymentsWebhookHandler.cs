using Fcg.Payment.Domain;
using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Application
{
    public class PaymentsWebhookHandler : IPaymentsWebhookHandler
    {
        private readonly IUnitOfWork _uow;
        private readonly IOutboxPublisher _outbox;

        public PaymentsWebhookHandler(IUnitOfWork uow, IOutboxPublisher outbox)
        {
            this._uow = uow;
            this._outbox = outbox;
        }

        public async Task HandleWebhookAsync(string rawBody, string signatureHeader, IPspClient psp, CancellationToken cancellationToken)
        {
            // Valida a assinatura do PSP
            if (!psp.TryValidateWebhookSignature(rawBody, signatureHeader))
                throw new UnauthorizedAccessException("Invalid signature");

            // Traduz o evento do PSP
            var (_, pspRef, status) = psp.ParseWebhook(rawBody);

            // Carrega o pagamento pelo pspReference
            var payment = await this._uow.PaymentRepository.GetByPspReferenceAsync(pspRef, cancellationToken)
                          ?? throw new InvalidOperationException("Payment not found");

            if (status is PaymentStatus.Captured or PaymentStatus.Authorized or PaymentStatus.Failed or PaymentStatus.Refunded)
            {
                payment.Status = status;
                payment.DateUpdated = DateTimeOffset.UtcNow;

                if (status == PaymentStatus.Captured)
                {
                    var evt = new
                    {
                        event_ = "payment.confirmed",
                        version = "1",
                        occurredAt = DateTimeOffset.UtcNow,
                        userId = payment.UserId,
                        purchaseId = payment.Id,
                        items = payment.Items.Select(i => new
                        {
                            gameId = i.GameId,
                            quantity = i.Quantity,
                            price = i.UnitPrice
                        })
                    };

                    this._outbox.Enqueue(evt, "payment.confirmed");
                }

                await this._uow.CommitAsync(cancellationToken);
            }
        }
    }
}
