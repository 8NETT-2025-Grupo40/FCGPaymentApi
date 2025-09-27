using System.Text.Json.Serialization;

namespace Fcg.Payment.Domain.Payments;

public class PaymentItem
{
    private PaymentItem() { }

    public PaymentItem(string gameId, decimal unitPrice)
    {
        this.GameId = gameId;
        this.UnitPrice = unitPrice;
    }

    public Guid PaymentId { get; private set; }
    public string GameId { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public decimal Total => this.UnitPrice;

	[JsonIgnore]
	public Payment Payment { get; private set; } = null!;
}