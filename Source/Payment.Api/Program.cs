using Microsoft.EntityFrameworkCore;
using Amazon.SQS;
using Fcg.Payment.Application;
using Fcg.Payment.Domain;
using Fcg.Payment.Infrastructure;
using Microsoft.OpenApi.Models;
using Fcg.Payment.Infrastructure.Messaging;
using Fcg.Payment.Infrastructure.PaymentServiceProvider;
using Fcg.Payment.API.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterServices();

// AWS Options + SQS client
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

// Services
builder.Services.Configure<PspOptions>(builder.Configuration.GetSection("Psp"));
builder.Services.AddHttpClient<IPspClient, HttpPspClientWireMock>();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddDbContextConfiguration(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCheck<DbContextHealthCheck<PaymentDbContext>>("DbContext_Check");

builder.ConfigureSerilog();

var app = builder.Build();

// TODO: Apagar
// Cria um escopo de serviço temporário para obter instâncias injetadas, como o DbContext.
using var scope = app.Services.CreateScope();

// Recupera o ApplicationDbContext (ou qualquer DbContext que você esteja usando) a partir do container de injeção de dependência.
var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

// Verifica se existem migrations pendentes (ainda não aplicadas ao banco de dados).
if (dbContext.Database.GetPendingMigrations().Any())
{
    // Aplica todas as migrations pendentes automaticamente ao banco de dados.
    dbContext.Database.Migrate();
}

app.ConfigureMiddlewares();

// Endpoints

// Health
app.MapGet("/", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow }));

// Create payment
app.MapPost("/payments", async (
    HttpRequest http,
    CreatePaymentRequest req,
    IPaymentAppService svc,
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
app.MapGet("/payments/{id:guid}", async (Guid id, IPaymentAppService service, CancellationToken cancellationToken) =>
{
    var response = await service.GetByIdAsync(id, cancellationToken);

    if (response == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(response);
})
.WithName("GetPayment")
.Produces(StatusCodes.Status404NotFound);

// Webhook do PSP
app.MapPost("/webhooks/psp", async (
    HttpRequest http,
    IPaymentsWebhookHandler svc,
    IPspClient psp,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(http.Body);
    var body = await reader.ReadToEndAsync(cancellationToken);
    var signature = http.Headers["X-PSP-Signature"].ToString();

    await svc.HandleWebhookAsync(body, signature, psp, cancellationToken);
    return Results.Ok();
})
.WithName("PspWebhook");

app.Run();
