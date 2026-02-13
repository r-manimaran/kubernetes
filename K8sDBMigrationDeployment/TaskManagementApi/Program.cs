using Microsoft.EntityFrameworkCore;
using TaskManagementApi;
using TaskManagementApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=taskmanagement.db";
    options.UseSqlServer(connectionString);
});

builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint(
    "/openapi/v1.json", "OpenAPI v1");
});
//app.UseSwagger();
//app.UseSwaggerUI();

app.UseHttpsRedirection();

// Map endpoints
app.MapUserEndpoints();
app.MapTaskEndpoints();
app.MapHealthEndpoints();

app.MapHealthChecks("/health");

app.Run();

