using System.Net.Http.Json;
using Fcg.Payment.Application;
using Microsoft.Extensions.Options;

namespace Fcg.Payment.Infrastructure.PaymentServiceProvider
{

    public class PspOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:8080"; // WireMock
    }

    public class HttpPspClientWireMock : IPspClient
    {
        private readonly HttpClient _http;
        private readonly PspOptions _opt;

        public HttpPspClientWireMock(HttpClient http, IOptions<PspOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
            _http.BaseAddress = new Uri(_opt.BaseUrl);
        }

        public async Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Domain.Payment payment, CancellationToken cancellationToken)
        {
            var payload = new
            {
                userId = payment.UserId,
                currency = payment.Currency,
                items = payment.Items.Select(i => new { gameId = i.GameId, quantity = i.Quantity, unitPrice = i.UnitPrice })
            };

            var resp = await _http.PostAsJsonAsync("/psp/checkout/sessions", payload, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: cancellationToken)
                       ?? throw new InvalidOperationException("PSP mock: resposta vazia");

            return (json.checkoutUrl!, json.pspReference!);
        }

        // WireMock não assina de fato — ok retornar true
        public bool TryValidateWebhookSignature(string payload, string signatureHeader) => true;

        public (string eventType, Domain.PaymentStatus status, string pspReference) Parse(string payload)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            var evt = doc.RootElement.GetProperty("eventType").GetString() ?? "unknown";
            var refId = doc.RootElement.GetProperty("pspReference").GetString() ?? "";
            var status = evt switch
            {
                "payment_captured" => Domain.PaymentStatus.Captured,
                "payment_failed" => Domain.PaymentStatus.Failed,
                "payment_refunded" => Domain.PaymentStatus.Refunded,
                _ => Domain.PaymentStatus.Pending
            };
            return (evt, status, refId);
        }

        public (string, string, Domain.PaymentStatus) ParseWebhook(string payload)
        {
            var (evt, status, refId) = Parse(payload);
            return (evt, refId, status);
        }

        private sealed record ResponseDto(string? sessionId, string? checkoutUrl, string? pspReference);
    }
}
