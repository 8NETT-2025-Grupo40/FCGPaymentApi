using Fcg.Payment.API.Domain;

namespace Fcg.Payment.API.Psp;

public class FakePspClient : IPspClient
{
    public Task<(string, string)> CreateCheckoutAsync(Domain.Payment payment, CancellationToken ct)
        => Task.FromResult(($"https://psp.example/checkout/{payment.PaymentId}", $"PSP-{payment.PaymentId:N}"));

    public Task CaptureAsync(string pspReference, CancellationToken ct) => Task.CompletedTask;

    public bool TryValidateWebhookSignature(string payload, string signatureHeader) => true;

    public (string, string, PaymentStatus) ParseWebhook(string payload)
        => ("payment_captured", "PSP-xyz", PaymentStatus.Captured);
}
