using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Domain;

public class Payment : BaseEntity
{
    public Payment()
    {
    }

    public Payment(Guid userId, decimal amount, string currency, List<PaymentItem> items)
    {
        this.UserId = userId;
        this.Amount = amount;
        this.Currency = currency;
        this.Items = items;
    }

    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PspReference { get; set; }
    public List<PaymentItem> Items { get; set; } = new();
}