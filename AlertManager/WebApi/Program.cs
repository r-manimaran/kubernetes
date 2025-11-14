using Prometheus;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpMetrics();

app.MapMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options => 
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI v1");
    });
}

app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();

