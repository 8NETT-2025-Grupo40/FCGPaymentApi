using Fcg.Payment.Domain;

namespace Fcg.Payment.Application;

public interface IPspClient
{
    Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Domain.Payment payment, CancellationToken cancellationToken);
    bool TryValidateWebhookSignature(string payload, string signatureHeader);
    (string eventType, string pspReference, PaymentStatus status) ParseWebhook(string payload);
}
