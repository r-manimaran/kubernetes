using EcommerceApp.Endpoints;
using EcommerceApp.Middleware;
using EcommerceApp.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
    options.IncludeScopes = true;
});

// -- OpenTelemetry (traces, metrics exposed on 9090 for Prometheus sidecar)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("ecommerce-api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddOtlpExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation()
                       .AddPrometheusExporter());
// -- App Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddProblemDetails();
//.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// -- Health checks (readiness + liveness probes for Kubernetes)
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapOrdersEndpoints();

// Kubernetes health check endpoints
app.MapHealthChecks("/healthz/ready");
app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapPrometheusScrapingEndpoint("/metrics"); // Sidecar scrapes this for Prometheus

app.Run();

