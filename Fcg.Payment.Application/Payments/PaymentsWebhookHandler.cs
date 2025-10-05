using Fcg.Payment.Application.Payments;
using Fcg.Payment.Application.Ports;
using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Common.EventSourcing;
using Fcg.Payment.Domain.Common.EventSourcing.Payment;
using Fcg.Payment.Domain.Payments;

public class PaymentsWebhookHandler : IPaymentsWebhookHandler
{
	private readonly IUnitOfWork _uow;
	private readonly IOutboxPublisher _outbox;

	public PaymentsWebhookHandler(IUnitOfWork uow, IOutboxPublisher outbox)
	{
		this._uow = uow;
		this._outbox = outbox;
	}

	public async Task HandleWebhookAsync(
		string rawBody,
		string signatureHeader,
		IPspClient psp,
		CancellationToken cancellationToken)
	{
		// Valida assinatura do PSP
		if (!psp.TryValidateWebhookSignature(rawBody, signatureHeader))
			throw new UnauthorizedAccessException("Invalid signature");

		// Parse do webhook do PSP
		(_, string pspRef, PaymentStatus pspStatus) = psp.ParseWebhook(rawBody);

		// Carrega o agregado pelo PSP reference
		Payment payment = await this._uow.PaymentRepository
							  .GetByPspReferenceAsync(pspRef, cancellationToken)
						  ?? throw new InvalidOperationException("Payment not found");

		// Se já estiver em estado final, retorna (idempotência)
		if (payment.Status is PaymentStatus.Captured or PaymentStatus.Failed or PaymentStatus.Refunded)
			return;

		bool publishConfirmation = false;

		var paymentEventType = PaymentEventType.PaymentCaptured;

		// Transições de acordo com o status informado pelo PSP
		switch (pspStatus)
		{
			case PaymentStatus.Authorized:
				payment.MarkAsAuthorized(pspRef);
				paymentEventType = PaymentEventType.PaymentAuthorized;

				break;

			case PaymentStatus.Captured:
				if (payment.MarkAsCaptured(pspRef))
					publishConfirmation = true;

				paymentEventType = PaymentEventType.PaymentCaptured;
				break;

			case PaymentStatus.Failed:
				payment.MarkAsFailed("PSP reported failure", pspRef);
				paymentEventType = PaymentEventType.PaymentFailed;
				break;

			case PaymentStatus.Refunded:
				payment.MarkAsRefunded(pspRef);
				paymentEventType = PaymentEventType.PaymentRefunded;
				break;

			case PaymentStatus.Pending:
			default:
				throw new InvalidOperationException($"PaymentStatus not found. Received: {pspStatus}");
		}

		// Publica evento de integração apenas quando Captured aconteceu agora
		if (publishConfirmation)
		{
			PaymentConfirmed evt = new PaymentConfirmed(
				PurchaseId: payment.Id,
				UserId: payment.UserId,
				Amount: payment.Amount,
				Currency: payment.Currency,
				OccurredAt: DateTimeOffset.UtcNow,
				Items: payment.Items
					.Select(i => new PaymentConfirmed.Item(i.GameId, i.UnitPrice))
					.ToList()
			);

			this._outbox.Enqueue(evt, "payment.confirmed");
		}

		var paymentUpdatedEvent = PaymentEvent.Create(payment, paymentEventType);

		await _uow.EventModelRepository.AddAsync(paymentUpdatedEvent, cancellationToken);

		await this._uow.CommitAsync(cancellationToken);
	}

	public sealed record PaymentConfirmed(
		Guid PurchaseId,
		Guid UserId,
		decimal Amount,
		string Currency,
		DateTimeOffset OccurredAt,
		IReadOnlyList<PaymentConfirmed.Item> Items)
	{
		public sealed record Item(string GameId, decimal Price);
	}

}
