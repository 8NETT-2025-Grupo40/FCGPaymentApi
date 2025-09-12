using Fcg.Payment.API.Domain;

namespace Fcg.Payment.API.Psp;

public interface IPspClient
{
    Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Domain.Payment payment, CancellationToken ct);
    Task CaptureAsync(string pspReference, CancellationToken ct);
    bool TryValidateWebhookSignature(string payload, string signatureHeader);
    (string eventType, string pspReference, PaymentStatus status) ParseWebhook(string payload);
}
