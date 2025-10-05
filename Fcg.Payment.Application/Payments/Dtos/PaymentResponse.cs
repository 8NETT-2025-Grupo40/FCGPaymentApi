using Fcg.Payment.Domain.Payments;
using System.Text.Json.Serialization;

namespace Fcg.Payment.Application.Payments.Dtos
{
    public class PaymentResponse
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentStatus Status { get; set; }
        public string? PspReference { get; set; }
        public List<PaymentItemResponse> Items { get; set; }

        [JsonConstructor]
		public PaymentResponse()
		{
			
		}

		public PaymentResponse(Domain.Payments.Payment payment)
        {
            this.PaymentId = payment.Id;
            this.UserId = payment.UserId;
            this.Amount = payment.Amount;
            this.Currency = payment.Currency;
            this.Status = payment.Status;
            this.PspReference = payment.PspReference;
            this.Items = payment.Items.Select(i => new PaymentItemResponse
            {
                GameId = i.GameId,
                UnitPrice = i.UnitPrice
            }).ToList();
        }
    }

    public class PaymentItemResponse
    {
        public string GameId { get; set; } = null!;
        public decimal UnitPrice { get; set; }
    }

}
