using System.Text.Json;

namespace Fcg.Payment.Domain.Common.EventSourcing.Payment;

public class PaymentEvent : EventModel
{
	public override Guid StreamId { get; set; }

	public PaymentEvent() : base() { }

	protected PaymentEvent(Guid streamId, string paymentCreatedObject, PaymentEventType eventType) : base(streamId, eventType, paymentCreatedObject) { }

	public static PaymentEvent Create(Payments.Payment payment, PaymentEventType eventType)
	{
		if (Guid.Empty == payment.Id)
		{
			throw new DomainException("Payment id must be defined");
		}

		var paymentObject = JsonSerializer.Serialize(payment);

		return new PaymentEvent(payment.Id, paymentObject, eventType);
	}
}
