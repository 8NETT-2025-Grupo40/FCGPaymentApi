namespace Fcg.Payment.Application;

public interface IPaymentsWebhookHandler
{
    Task HandleWebhookAsync(string rawBody, string signatureHeader, IPspClient psp, CancellationToken cancellationToken);
}