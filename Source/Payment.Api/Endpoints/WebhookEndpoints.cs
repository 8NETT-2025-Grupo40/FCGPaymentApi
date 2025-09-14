using Fcg.Payment.Application.Payments;
using Fcg.Payment.Application.Ports;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Fcg.Payment.API.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        // /webhooks
        RouteGroupBuilder webhooks = app.MapGroup("/webhooks")
                          .WithTags("Webhooks");

        webhooks.MapPost("/psp", ReceivePspWebhook)
            .WithName("PspWebhook")
            .WithSummary("Receive notifications from PSP (webhook).")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi(op =>
            {
                // Documenta o header de assinatura do PSP
                op.Parameters ??= new List<OpenApiParameter>();
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-PSP-Signature",
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Signature sent by the PSP to validate the webhook.",
                    Schema = new OpenApiSchema { Type = "string" }
                });
                return op;
            });
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> ReceivePspWebhook(
        HttpRequest http,
        [FromHeader(Name = "X-PSP-Signature")] string? signature,
        IPaymentsWebhookHandler handler,
        IPspClient psp,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return TypedResults.Unauthorized();
        }

        // Precisa do corpo bruto para validar assinatura
        using StreamReader reader = new(http.Body);
        string rawBody = await reader.ReadToEndAsync(ct);

        // O handler valida a assinatura (via IPspClient) e processa
        await handler.HandleWebhookAsync(rawBody, signature, psp, ct);

        return TypedResults.Ok();
    }
}
