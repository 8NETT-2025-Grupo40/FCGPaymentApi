using Microsoft.EntityFrameworkCore;
using Amazon.SQS;
using Fcg.Payment.API.Application;
using Fcg.Payment.API.Contracts;
using Fcg.Payment.API.Domain;
using Fcg.Payment.API.Infrastructure;
using Fcg.Payment.API.Psp;
using Fcg.Payment.API.Services;
using Microsoft.OpenApi.Models;

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
builder.Services.Configure<PspOptions>(builder.Configuration.GetSection("Psp"));
builder.Services.AddHttpClient<IPspClient, HttpPspClient>();

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
    Payment? p = await db.Payments.Include(x => x.Items).FirstOrDefaultAsync(x => x.PaymentId == id, ct);


    if (p == null)
    {
        return Results.NotFound();
    }

    PaymentResponse response = new(p);
    return Results.Ok(response);
})
.WithName("GetPayment")
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
