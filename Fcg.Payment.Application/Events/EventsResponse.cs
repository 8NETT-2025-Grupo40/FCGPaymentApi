using System.Text.Json.Serialization;

namespace Fcg.Payment.Application.Events;

public class EventsResponse(Guid paymentIdentifier, Guid userId, IReadOnlyCollection<EventDetailResponse> events)
{
	[JsonPropertyName("PaymentId")]
	public Guid PaymentIdentifier { get; set; } = paymentIdentifier;

	[JsonPropertyName("UserId")]
	public Guid UserId { get; set; } = userId;

	[JsonPropertyName("Events")]
	public IReadOnlyCollection<EventDetailResponse> Events { get; set; } = events;
}