using Microsoft.EntityFrameworkCore;
using OrdersApi;
using OrdersApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("OrdersDb")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint(
    "/openapi/v1.json", "OpenAPI v1");
});
// This will perform the migrate when called as a job in Kubernetes
if (args.Contains("migrate"))
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Starting database migration...");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed.");
        throw;
    }
    return;
}

// app.UseHttpsRedirection();

app.MapGet("/orders", async (AppDbContext db) =>
{
    return await db.Orders.ToListAsync();
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.MapPost("/orders", async (Order order, AppDbContext db) =>
{
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Ok(order);
});

app.Run();

