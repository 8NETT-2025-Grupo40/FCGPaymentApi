using Fcg.Payment.Application.Payments.Dtos;
using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Payments;
using System.Text.Json.Serialization;

namespace Fcg.Payment.Application.Events;

public class EventPaymentResponse(
	PaymentStatus status,
	decimal amount,
	string currency,
	string paymentServiceProviderReference,
	List<PaymentItemResponse> items)
{
	[JsonPropertyName("Status")]
	public string Status { get; set; } = ConvertPaymentStatusToString(status);

	[JsonPropertyName("Amount")]
	public decimal Amount { get; set; } = amount;

	[JsonPropertyName("Currency")]
	public string Currency { get; set; } = currency;

	[JsonPropertyName("PaymentServiceProviderReference")]
	public string PaymentServiceProviderReference { get; set; } = paymentServiceProviderReference;

	[JsonPropertyName("Items")]
	public List<PaymentItemResponse> Items { get; set; } = items;

	private static string ConvertPaymentStatusToString(PaymentStatus status)
	{
		switch(status)
		{
			case PaymentStatus.Pending:
				return "Pending";
			case PaymentStatus.Authorized:
				return "Authorized";
			case PaymentStatus.Captured:
				return "Captured";
			case PaymentStatus.Refunded:
				return "Refunded";
			case PaymentStatus.Failed:
				return "Failed";
			default:
				throw new DomainException("Status not found");
		
		}
	}
}
