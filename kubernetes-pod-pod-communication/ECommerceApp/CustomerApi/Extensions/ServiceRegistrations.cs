using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
namespace CustomerApi.Externsions;

public static class ServiceRegistrations
{
    public static void ConfigureOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService("CustomerApi");
            })
            .WithTracing(tracing =>
            {
                // Automatically collect traces from ASP.NET Core, HttpClient, and other libraries
                tracing.AddAspNetCoreInstrumentation();

                // Automatically collect traces from outgoing HTTP requests
                tracing.AddHttpClientInstrumentation();

                // Export traces to the console (for debugging purposes)
                tracing.AddConsoleExporter();

                // Export traces using OTLP (OpenTelemetry Protocol) to a collector or backend
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4317"); // Replace with your OTLP endpoint
                });
            })
            .WithMetrics(metrics =>
            {                 // Automatically collect metrics from ASP.NET Core, HttpClient, and other libraries
                metrics.AddAspNetCoreInstrumentation();
                // Automatically collect metrics from outgoing HTTP requests
                metrics.AddHttpClientInstrumentation();
                // Export metrics to the console (for debugging purposes)
                metrics.AddConsoleExporter();
                // Export metrics using OTLP (OpenTelemetry Protocol) to a collector or backend
                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4317"); // Replace with your OTLP endpoint
                });
            });
    
    }
}
