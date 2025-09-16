using Fcg.Payment.API.Middlewares;
using Serilog;

namespace Fcg.Payment.API.Setup;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddlewares(this WebApplication app)
    {
        app.UseMiddleware<GlobalErrorHandlingMiddleware>();
        app.UseMiddleware<StructuredLogMiddleware>();

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseSwaggerConfiguration();

        return app;
    }
}