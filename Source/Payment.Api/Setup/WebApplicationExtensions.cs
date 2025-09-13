using Serilog;

namespace Fcg.Payment.API.Setup;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddlewares(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseSwaggerConfiguration();

        return app;
    }
}