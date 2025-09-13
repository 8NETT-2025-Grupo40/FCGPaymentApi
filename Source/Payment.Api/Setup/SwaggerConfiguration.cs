using System.Text.Json;
using Fcg.Payment.Application;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fcg.Payment.API.Setup;

public static class SwaggerConfiguration
{
    public static void AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG API Payment", Version = "v1" });
            c.SchemaFilter<CreatePaymentRequestSchemaFilter>();
        });
    }

    public static void UseSwaggerConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Documentation for FCG Payment Project");
                c.RoutePrefix = string.Empty;
            });
        }
    }

    private class CreatePaymentRequestSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(CreatePaymentRequest) )
            {
                string jsonName = JsonNamingPolicy.CamelCase.ConvertName(nameof(CreatePaymentRequest.Currency));
                // No Swagger, define o valor padrão e exemplo para "Currency" como "BRL". Isso facilita alguns testes manuais.
                if (schema.Properties.TryGetValue(jsonName, out var currencyProp))
                {
                    currencyProp.Default = new OpenApiString("BRL");
                    currencyProp.Example = new OpenApiString("BRL");
                }
            }
        }
    }
}