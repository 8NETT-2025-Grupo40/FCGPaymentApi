using Payment.Api.Domain;

namespace Payment.Api.Application
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

        public PaymentResponse(Domain.Payment payment)
        {
            this.PaymentId = payment.PaymentId;
            this.UserId = payment.UserId;
            this.Amount = payment.Amount;
            this.Currency = payment.Currency;
            this.Status = payment.Status;
            this.PspReference = payment.PspReference;
            this.Items = payment.Items.Select(i => new PaymentItemResponse
            {
                PaymentId = i.PaymentId,
                GameId = i.GameId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();
        }
    }

    public class PaymentItemResponse
    {
        public Guid PaymentId { get; set; }
        public string GameId { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

}
