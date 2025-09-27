using Fcg.Payment.Application.Ports;

namespace Fcg.Payment.Application.Payments;

public interface IPaymentsWebhookHandler
{
    Task HandleWebhookAsync(string rawBody, string signatureHeader, IPspClient psp, CancellationToken cancellationToken);
}