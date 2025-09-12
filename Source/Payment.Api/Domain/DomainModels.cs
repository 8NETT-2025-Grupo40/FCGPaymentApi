namespace Fcg.Payment.API.Domain;

public enum PaymentStatus { Pending, Authorized, Captured, Failed, Refunded }

public class Payment
{
    public Guid PaymentId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PspReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<PaymentItem> Items { get; set; } = new();
}

public class PaymentItem
{
    public Guid PaymentId { get; set; }
    public string GameId { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Payment? Payment { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
}

public class IdempotencyKey
{
    public string Key { get; set; } = default!;
    public string PayloadHash { get; set; } = default!;
    public string ResponseBody { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
