using BlogsApi.Data;
using BlogsApi.Validations;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using BlogsApi.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using BlogsApi.ExceptionHandler;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.File("log/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
builder.Host.UseSerilog();

Log.Logger.Information("Application is building...");

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("Blogs"));

builder.Services.AddValidatorsFromAssemblyContaining<CreatePostValidator>();

builder.Services.AddProblemDetails(options => {
    options.CustomizeProblemDetails = (context) =>
    {
        Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;

        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions = new Dictionary<string, object?>()
            {
                {"requestId", context.HttpContext.TraceIdentifier},
                {"traceId", activity?.Id},
                {"spanId", activity?.SpanId.ToString()}
            };
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(),
        tags: new[] { "live" });


builder.Logging.AddOpenTelemetry(logging =>
{
logging.IncludeFormattedMessage = true;
logging.IncludeScopes = true;
});


builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("BlogPostApi"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri("http://localhost:4317");
                opt.Protocol = OtlpExportProtocol.Grpc;
            })
            .AddConsoleExporter(opt =>
            {
                opt.Targets = ConsoleExporterOutputTargets.Console;
            });
    });


 var app = builder.Build();

// use serilog
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => {
        options.SwaggerEndpoint(
        "/openapi/v1.json", "OpenAPI v1");
    });
}
// configure health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.UseExceptionHandler();

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Log.Logger.Information("Application is running...");

app.Run();
