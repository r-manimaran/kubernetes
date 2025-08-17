using Serilog;
using System.Reflection;
using Serilog.Debugging;

SelfLog.Enable(Console.Error);

var builder = WebApplication.CreateBuilder(args);

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

builder.Host.UseSerilog((context, services, config) =>config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
    .Enrich.WithProperty("Version", version)
    .ReadFrom.Services(services));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
