using CustomerApi.Endpoints;
using CustomerApi.Externsions;

var builder = WebApplication.CreateBuilder(args);

// Register Health Checks
builder.Services.AddHealthChecks();

// Register OpenTelemetry services for tracing and metrics
builder.Services.ConfigureOpenTelemetry();

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});


var app = builder.Build();

// Liveness and Readiness endpoints
app.MapHealthChecks("/health/live"); // Indicates that the application is running and can respond to requests
app.MapHealthChecks("/health/ready"); // Indicates that the application is ready to handle requests and has all necessary dependencies available

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapCustomerEndpoints();

app.Run();
