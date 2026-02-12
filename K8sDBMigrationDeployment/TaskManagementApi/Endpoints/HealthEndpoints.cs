namespace TaskManagementApi.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/Apphealth", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

        app.MapGet("/health/ready",async (ApplicationDbContext db) =>
        {
            try
            {
                // Check database connectivity
                await db.Database.CanConnectAsync();
                return Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return Results.Json(new { status = "Unhealthy", error = ex.Message, timestamp = DateTime.UtcNow }, statusCode: 503);
            }
        });
    }


}
