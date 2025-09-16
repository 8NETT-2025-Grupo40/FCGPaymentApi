using Microsoft.EntityFrameworkCore;
using Amazon.SQS;
using Fcg.Payment.API.Endpoints;
using Fcg.Payment.Infrastructure;
using Fcg.Payment.Infrastructure.PaymentServiceProvider;
using Fcg.Payment.API.Setup;
using Serilog;
using Fcg.Payment.Application.Ports;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.RegisterServices();
builder.Services.RegisterMiddlewares();
// AWS Options + SQS client
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

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
app.MapHealthCheckEndpoints();
app.MapPaymentEndpoints();
app.MapWebhookEndpoints();

app.Run();
