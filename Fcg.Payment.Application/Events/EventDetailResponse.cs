using Fcg.Payment.Application.Payments.Dtos;
using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Common.EventSourcing;
using System.Text.Json.Serialization;

namespace Fcg.Payment.Application.Events;

public class EventDetailResponse(EventPaymentResponse paymentObject, PaymentEventType eventType, DateTimeOffset eventTime)
{
	[JsonPropertyName("PaymentObject")]
	public EventPaymentResponse PaymentObject { get; set; } = paymentObject;

	[JsonPropertyName("EventType")]
	public string EventType { get; set; } = ConvertEventType(eventType);

	[JsonPropertyName("EventTime")]
	public DateTimeOffset EventTime { get; set; } = eventTime;

	[JsonPropertyName("EventTimestamp")]
	public long EventTimestamp { get; set; } = eventTime.ToUnixTimeSeconds();

	private static string ConvertEventType(PaymentEventType paymentEventType)
	{
		return paymentEventType switch
		{
			PaymentEventType.PaymentCreated => "Payment created",
			PaymentEventType.PspPaymentBinded => "Payment service provider binded to payment",
			PaymentEventType.PaymentAuthorized => "Payment authorized",
			PaymentEventType.PaymentCaptured => "Payment captured",
			PaymentEventType.PaymentFailed => "Payment failed",
			PaymentEventType.PaymentRefunded => "Payment refunded",
			_ => throw new DomainException("Invalid type"),
		};
	}
}
