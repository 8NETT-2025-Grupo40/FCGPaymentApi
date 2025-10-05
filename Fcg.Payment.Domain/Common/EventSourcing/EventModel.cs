namespace Fcg.Payment.Domain.Common.EventSourcing;
public abstract class EventModel : BaseEntity
{
	public abstract Guid StreamId { get; set; } // This equals to each payment id (e.g.).
	public string EventData { get; set; } // This represents the event object in json.
	public PaymentEventType EventType { get; set; }

	protected EventModel()
	{
		
	}

	protected EventModel(Guid streamId, PaymentEventType type, string data)
	{
		StreamId = streamId;
		DateCreated = DateTime.Now;
		EventData = data;
		EventType = type;
	}
}