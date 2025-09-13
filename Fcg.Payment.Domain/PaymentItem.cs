namespace Fcg.Payment.Domain;

public class PaymentItem
{
    private PaymentItem() { }

    public PaymentItem(string gameId, decimal unitPrice)
    {
        GameId = gameId;
        UnitPrice = unitPrice;
    }

    public Guid PaymentId { get; private set; }
    public string GameId { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public decimal Total => this.UnitPrice;

    public Payment Payment { get; private set; } = null!;
}