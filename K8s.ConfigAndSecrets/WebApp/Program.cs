using WebApp;
using WebApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Configuration from mounted files
builder.Configuration.AddJsonFile("/app/config/appsettings.Production.json", optional: true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// configure app settings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));


var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("App Name:{AppName}", Environment.GetEnvironmentVariable("APP_NAME") ?? "Default AppName");
logger.LogInformation("Environment:{Environment}", Environment.GetEnvironmentVariable("ENVIRONMENT")??"Dev");
logger.LogInformation("Feature Flag Weather:{WeatherFlag}", Environment.GetEnvironmentVariable("FEATURE_FLAG_WEATHER") ?? "false");
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
