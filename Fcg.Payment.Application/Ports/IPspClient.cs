using Fcg.Payment.Domain.Payments;

namespace Fcg.Payment.Application.Ports;

public interface IPspClient
{
    Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Domain.Payments.Payment payment, CancellationToken cancellationToken);
    bool TryValidateWebhookSignature(string payload, string signatureHeader);
    (string eventType, string pspReference, PaymentStatus status) ParseWebhook(string payload);
}
