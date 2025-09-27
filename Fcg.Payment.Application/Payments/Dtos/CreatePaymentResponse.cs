namespace Fcg.Payment.Application.Payments.Dtos;

public record CreatePaymentResponse(Guid PaymentId, string CheckoutUrl);