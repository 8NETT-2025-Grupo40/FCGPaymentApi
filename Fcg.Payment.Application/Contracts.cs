namespace Fcg.Payment.Application;

public record CreatePaymentItem(string GameId, decimal UnitPrice);
public record CreatePaymentRequest(Guid UserId, IEnumerable<CreatePaymentItem> Items, string Currency = "BRL");
public record CreatePaymentResponse(Guid PaymentId, string CheckoutUrl);
