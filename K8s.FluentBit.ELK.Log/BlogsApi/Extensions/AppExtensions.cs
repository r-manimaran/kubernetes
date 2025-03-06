namespace BlogsApi.Extensions;

public static class AppExtensions
{
    public static void ApplySeeding(this IApplicationBuilder app)
    {
        var scope = app.ApplicationServices.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        seeder.SeedData();
    }    
}
