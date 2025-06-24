using HealthCheckDemo;
using HealthCheckDemo.Data;
using HealthCheckDemo.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MonitoringDb")));
builder.Services.AddHostedService<MonitoringBackgroundService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit");
        opt.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:Window"));
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit");
    });
});

builder.Services.AddHealthChecks()
                .AddCheck<WebsiteHealthCheck>("api_health_check", tags: new[] { "api" })
                .AddCheck<DatabaseHealthCheck>("database_health_check", tags: new[] { "database" });

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(builder.Configuration.GetValue<int>("HealthChecksUI:EvaluationTimeInSeconds"));
    setup.SetMinimumSecondsBetweenFailureNotifications(builder.Configuration.GetValue<int>("HealthChecksUI:MinimumSecondsBetweenFailureNotifications"));
    setup.AddHealthCheckEndpoint("API", "/health");
}).AddInMemoryStorage();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false,
    Predicate = _ => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    }
}).RequireRateLimiting("fixed");

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.AddCustomStylesheet("wwwroot/css/health-checks.css");
    options.PageTitle = "API Health Monitoring";
    options.AsideMenuOpened = true;
});
app.Run();
