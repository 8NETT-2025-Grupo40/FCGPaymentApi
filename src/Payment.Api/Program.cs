using Microsoft.EntityFrameworkCore;
using Amazon.SQS;
using Microsoft.OpenApi.Models;
using Payment.Api.Infrastructure;
using Payment.Api.Services;
using Payment.Api.Psp;
using Payment.Api.Contracts;
using Payment.Api.Domain;
using Payment.Api.Application;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<PaymentsDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// AWS Options + SQS client
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

// Services
builder.Services.AddScoped<PaymentService>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.Configure<Payment.Api.Psp.PspOptions>(builder.Configuration.GetSection("Psp"));
builder.Services.AddHttpClient<Payment.Api.Psp.IPspClient, Payment.Api.Psp.HttpPspClient>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health
app.MapGet("/", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow }));

// Create payment
app.MapPost("/payments", async (
    HttpRequest http,
    CreatePaymentRequest req,
    PaymentService svc,
    IPspClient psp,
    CancellationToken ct) =>
{
    if (!http.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
        return Results.BadRequest(new { message = "Missing Idempotency-Key" });

    var resp = await svc.CreateAsync(req, key!, psp, ct);
    return Results.Created($"/payments/{resp.PaymentId}", resp);
})
.WithOpenApi(op =>
{
    op.Parameters ??= new List<OpenApiParameter>();
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "Idempotency-Key",
        In = ParameterLocation.Header,
        Required = true,
        Description = "Chave de idempotência para tornar o POST seguro a retries.",
        Schema = new OpenApiSchema { Type = "string" }
    });
    return op;
})
.WithName("CreatePayment")
.Produces<CreatePaymentResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

// Get payment
app.MapGet("/payments/{id:guid}", async (Guid id, PaymentsDbContext db, CancellationToken ct) =>
{
    Payment.Api.Domain.Payment? p = await db.Payments.Include(x => x.Items).FirstOrDefaultAsync(x => x.PaymentId == id, ct);


    if (p == null)
    {
        return Results.NotFound();
    }

    PaymentResponse response = new(p);
    return Results.Ok(response);
})
.WithName("GetPayment")
.Produces(StatusCodes.Status404NotFound);

// Refund (simplificado)
app.MapPost("/payments/{id:guid}/refund", async (Guid id, PaymentsDbContext db, IPspClient psp, CancellationToken ct) =>
{
    var p = await db.Payments.FirstOrDefaultAsync(x => x.PaymentId == id, ct);
    if (p is null) return Results.NotFound();

    p.Status = PaymentStatus.Refunded;
    p.UpdatedAt = DateTimeOffset.UtcNow;

    db.Outbox.Add(new OutboxMessage
    {
        Type = "payment.refunded",
        PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { @event = "payment.refunded", purchaseId = p.PaymentId, userId = p.UserId })
    });

    await db.SaveChangesAsync(ct);
    return Results.Ok();
})
.WithName("RefundPayment")
.Produces(StatusCodes.Status404NotFound);

// Webhook do PSP
app.MapPost("/webhooks/psp", async (
    HttpRequest http,
    PaymentService svc,
    IPspClient psp,
    CancellationToken ct) =>
{
    using var reader = new StreamReader(http.Body);
    var body = await reader.ReadToEndAsync(ct);
    var signature = http.Headers["X-PSP-Signature"].ToString();

    await svc.HandleWebhookAsync(body, signature, psp, ct);
    return Results.Ok();
})
.WithName("PspWebhook");

app.Run();
