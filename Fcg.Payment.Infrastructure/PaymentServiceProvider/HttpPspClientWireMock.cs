using System.Net.Http.Json;
using Fcg.Payment.Application.Ports;
using Fcg.Payment.Domain.Payments;
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
            this._http = http;
            this._opt = opt.Value;
            this._http.BaseAddress = new Uri(this._opt.BaseUrl);
        }

        public async Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Domain.Payments.Payment payment, CancellationToken cancellationToken)
        {
            var payload = new
            {
                userId = payment.UserId,
                currency = payment.Currency,
                items = payment.Items.Select(i => new { gameId = i.GameId, unitPrice = i.UnitPrice })
            };

            var resp = await this._http.PostAsJsonAsync("/psp/checkout/sessions", payload, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: cancellationToken)
                       ?? throw new InvalidOperationException("PSP mock: resposta vazia");

            return (json.checkoutUrl!, json.pspReference!);
        }

        // WireMock não assina de fato — ok retornar true
        public bool TryValidateWebhookSignature(string payload, string signatureHeader) => true;

        public (string eventType, PaymentStatus status, string pspReference) Parse(string payload)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            var evt = doc.RootElement.GetProperty("eventType").GetString() ?? "unknown";
            var refId = doc.RootElement.GetProperty("pspReference").GetString() ?? "";
            var status = evt switch
            {
                "payment_captured" => PaymentStatus.Captured,
                "payment_failed" => PaymentStatus.Failed,
                "payment_refunded" => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending
            };
            return (evt, status, refId);
        }

        public (string, string, PaymentStatus) ParseWebhook(string payload)
        {
            var (evt, status, refId) = this.Parse(payload);
            return (evt, refId, status);
        }

        private sealed record ResponseDto(string? sessionId, string? checkoutUrl, string? pspReference);
    }
}
