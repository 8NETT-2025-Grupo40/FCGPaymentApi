using Fcg.Payment.Application.Payments;
using Fcg.Payment.Application.Payments.Dtos;
using Fcg.Payment.Application.Ports;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Fcg.Payment.API.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        // /payments
        var payments = app.MapGroup("/payments")
            .WithTags("Payments");

        payments.MapPost("/", CreatePayment)
            .WithName(nameof(CreatePayment))
            .WithSummary("Create a new payment (idempotent via header)")
            .Produces<CreatePaymentResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi(op =>
            {
                // Documenta o header Idempotency-Key
                OpenApiParameter? idempotency = op.Parameters.FirstOrDefault(p => p.Name == "Idempotency-Key");
                if (idempotency != null)
                {
                    idempotency.Description = "Idempotency key to make POST safe for retries.";
                }

                return op;
            });

        payments.MapGet("/{id:guid}", GetPaymentById)
            .WithName(nameof(GetPaymentById))
            .WithSummary("Get payment by id");
    }

    private static async Task<Results<CreatedAtRoute<CreatePaymentResponse>, BadRequest<object>>> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        IPaymentAppService service,
        IPspClient psp,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return TypedResults.BadRequest<object>(new { message = "Missing Idempotency-Key" });
        }

        CreatePaymentResponse resp = await service.CreateAsync(request, idempotencyKey, psp, ct);

        // Usa o nome do GET para construir o Location header
        return TypedResults.CreatedAtRoute(
            routeName: nameof(GetPaymentById),
            routeValues: new { id = resp.PaymentId },
            value: resp
        );
    }

    private static async Task<Results<Ok<PaymentResponse>, NotFound>> GetPaymentById(
        Guid id,
        IPaymentAppService service,
        CancellationToken ct)
    {
        var response = await service.GetByIdAsync(id, ct);

        return response is not null
            ? TypedResults.Ok(response)
            : TypedResults.NotFound();
    }
}
