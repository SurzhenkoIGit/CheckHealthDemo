using HealthCheckDemo.Data;
using HealthCheckDemo.Models;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualBasic;

namespace HealthCheckDemo.Services
{
    public class MonitoringBackgroundService : BackgroundService
    {
        private readonly ILogger<MonitoringBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public MonitoringBackgroundService(ILogger<MonitoringBackgroundService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
                    var healthCheck = scope.ServiceProvider.GetRequiredService<WebsiteHealthCheck>();

                    var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

                    var metric = new MonitoringMetric
                    {
                        Timestamp = DateTime.UtcNow,
                        MetricName = "SiteHealth",
                        Value = result.Status == HealthStatus.Healthy ? 1 : 0,
                        Unit = "Status",
                        Status = result.Status.ToString()
                    };

                    dbContext.Metrics.Add(metric);

                    if (result.Status != HealthStatus.Healthy)
                    {
                        var error = new SiteError
                        {
                            Timestamp = DateTime.UtcNow,
                            ErrorType = result.Status.ToString(),
                            Message = result.Description,
                            Source = "HealthCheck",
                            StatusCode = result.Data.ContainsKey("Status") ? result.Data["Status"].ToString() : "Unknown",
                            ResponseTime = result.Data.ContainsKey("ResponseTime") ? Convert.ToInt64(result.Data["ResponseTime"]) : 0,
                        };

                        dbContext.Errors.Add(error);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);

                    if (DateTime.UtcNow.Hour == 0)
                        await CleanupOldData(dbContext, stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(_configuration.GetValue<int>("MonitoringCheckIntervalSeconds", 60)), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during monitoring execution");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task CleanupOldData(MonitoringDbContext context, CancellationToken stoppingToken)
        {
            try
            {
                var retentionDays = _configuration.GetValue<int>("Monitoring:RetentionDays", 30);
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                //удаляются старые ошибки
                await context.Errors.Where(e => e.Timestamp < cutoffDate).ExecuteDeleteAsync(stoppingToken);

                //удаляются старые метрики
                await context.Metrics.Where(m => m.Timestamp < cutoffDate).ExecuteDeleteAsync(stoppingToken);

                _logger.LogInformation("Cleaned up data older then {CutoffDate}", cutoffDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during old data cleanup");
            }
        }
    }
}
