using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

namespace Fcg.Payment.API.Setup;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }
}