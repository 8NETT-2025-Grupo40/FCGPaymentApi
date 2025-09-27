using System.Net.Http.Json;
using Fcg.Payment.Application.Ports;
using Fcg.Payment.Domain.Payments;
using Microsoft.Extensions.Configuration;

namespace Fcg.Payment.Infrastructure.PaymentServiceProvider
{
    public class HttpPspClientWireMock : IPspClient
    {
        private readonly HttpClient _http;

        public HttpPspClientWireMock(HttpClient http, IConfiguration cfg)
        {
            this._http = http;

            string? wiremockUrl = cfg["Psp:WIREMOCK_URL"];
            if (string.IsNullOrEmpty(wiremockUrl))
            {
                throw new InvalidOperationException("WIREMOCK_URL not provided");
            }

            this._http.BaseAddress = new Uri(wiremockUrl);
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

        public bool TryValidateWebhookSignature(string payload, string signatureHeader)
            //  WireMock não assina, retorna true direto
            => true;

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
