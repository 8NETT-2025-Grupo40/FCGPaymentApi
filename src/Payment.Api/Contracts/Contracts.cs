namespace Payment.Api.Contracts;

public record CreatePaymentItem(string GameId, int Quantity, decimal UnitPrice);
public record CreatePaymentRequest(Guid UserId, IEnumerable<CreatePaymentItem> Items, string Currency = "BRL");
public record CreatePaymentResponse(Guid PaymentId, string CheckoutUrl);
