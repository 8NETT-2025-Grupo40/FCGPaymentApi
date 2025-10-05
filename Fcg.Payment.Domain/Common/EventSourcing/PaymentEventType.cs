namespace Fcg.Payment.Domain.Common.EventSourcing;

public enum PaymentEventType
{
	PaymentCreated = 0,
	PspPaymentBinded,
	PaymentAuthorized,
	PaymentCaptured,
	PaymentFailed,
	PaymentRefunded
}
