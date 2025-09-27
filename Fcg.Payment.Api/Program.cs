using Amazon.SQS;
using Fcg.Payment.API.Endpoints;
using Fcg.Payment.API.Setup;
using Fcg.Payment.Application.Ports;
using Fcg.Payment.Infrastructure;
using Fcg.Payment.Infrastructure.PaymentServiceProvider;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Services
builder.ConfigureSerilog();
builder.Services.SetupOpenTelemetry();

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

var app = builder.Build();

app.ConfigureMiddlewares();

// Endpoints
app.MapHealthCheckEndpoints();
app.MapPaymentEndpoints();
app.MapWebhookEndpoints();

app.Run();
