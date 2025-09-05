using Microsoft.Extensions.Options;

namespace Payment.Api.Psp
{

    public class PspOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:8080"; // WireMock
    }

    public class HttpPspClient : IPspClient
    {
        private readonly HttpClient _http;
        private readonly PspOptions _opt;

        public HttpPspClient(HttpClient http, IOptions<PspOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
            _http.BaseAddress = new Uri(_opt.BaseUrl);
        }

        public async Task<(string checkoutUrl, string pspRef)> CreateCheckoutAsync(Domain.Payment payment, CancellationToken ct)
        {
            var payload = new
            {
                userId = payment.UserId,
                currency = payment.Currency,
                items = payment.Items.Select(i => new { gameId = i.GameId, quantity = i.Quantity, unitPrice = i.UnitPrice })
            };

            var resp = await _http.PostAsJsonAsync("/psp/checkout/sessions", payload, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: ct)
                       ?? throw new InvalidOperationException("PSP mock: resposta vazia");

            return (json.checkoutUrl!, json.pspReference!);
        }

        public Task CaptureAsync(string pspReference, CancellationToken ct) => Task.CompletedTask;

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

        // compat com sua interface atual
        public (string, string, Domain.PaymentStatus) ParseWebhook(string payload)
        {
            var (evt, status, refId) = Parse(payload);
            return (evt, refId, status);
        }

        private sealed record ResponseDto(string? sessionId, string? checkoutUrl, string? pspReference);
    }
}
