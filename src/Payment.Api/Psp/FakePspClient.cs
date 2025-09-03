namespace Payment.Api.Psp;

public class FakePspClient : IPspClient
{
    public Task<(string, string)> CreateCheckoutAsync(Payment.Api.Domain.Payment payment, CancellationToken ct)
        => Task.FromResult(($"https://psp.example/checkout/{payment.PaymentId}", $"PSP-{payment.PaymentId:N}"));

    public Task CaptureAsync(string pspReference, CancellationToken ct) => Task.CompletedTask;

    public bool TryValidateWebhookSignature(string payload, string signatureHeader) => true;

    public (string, string, Payment.Api.Domain.PaymentStatus) ParseWebhook(string payload)
        => ("payment_captured", "PSP-xyz", Payment.Api.Domain.PaymentStatus.Captured);
}
