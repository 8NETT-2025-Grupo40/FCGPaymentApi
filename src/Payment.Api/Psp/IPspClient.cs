namespace Payment.Api.Psp;

public interface IPspClient
{
    Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Payment.Api.Domain.Payment payment, CancellationToken ct);
    Task CaptureAsync(string pspReference, CancellationToken ct);
    bool TryValidateWebhookSignature(string payload, string signatureHeader);
    (string eventType, string pspReference, Payment.Api.Domain.PaymentStatus status) ParseWebhook(string payload);
}
