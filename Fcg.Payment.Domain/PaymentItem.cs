namespace Fcg.Payment.Domain;

public class PaymentItem
{
    public Guid PaymentId { get; set; }
    public string GameId { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Payment? Payment { get; set; }
}