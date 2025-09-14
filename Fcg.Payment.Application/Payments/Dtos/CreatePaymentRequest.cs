namespace Fcg.Payment.Application.Payments.Dtos;

public record CreatePaymentRequest(Guid UserId, IEnumerable<CreatePaymentItem> Items, string Currency = "BRL");