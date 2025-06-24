using HealthCheckDemo.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace HealthCheckDemo
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly MonitoringDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(MonitoringDbContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                if (!canConnect)
                    return HealthCheckResult.Unhealthy("Cannot connected to database");

                //Проверка наличия ожидающих миграций
                var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

                if (pendingMigrations.Any())
                {
                    return HealthCheckResult.Degraded($"Database needs {pendingMigrations.Count} migrations",
                        data: new Dictionary<string, object>
                        {
                            { "PendingMigrations", string.Join(",", pendingMigrations) }
                        });
                }

                var watch = Stopwatch.StartNew();
                await _context.Metrics.OrderByDescending(m => m.Timestamp).Take(1).ToListAsync(cancellationToken);
                watch.Stop();

                var data = new Dictionary<string, object>
                {
                    { "ResponseTime", watch.ElapsedMilliseconds },
                    { "DatabaseSize", await GetDatabaseSize() },
                    { "ConnectionString", _context.Database.GetConnectionString() }
                };

                if(watch.ElapsedMilliseconds > 1000)
                {
                    return HealthCheckResult.Degraded($"Database response time is high: {watch.ElapsedMilliseconds}",
                        data: data);
                }

                return HealthCheckResult.Healthy("Database is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database health check failed", exception: ex);
            }
        }

        private async Task<object> GetDatabaseSize()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();

                if (connectionString == null)
                    return "Unknown";

                var builder = new SqliteConnectionStringBuilder(connectionString);
                var dbPath = builder.DataSource;

                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    var sizeInMb = fileInfo.Length / (1024.0 * 1024.0);
                    return $"{sizeInMb:F2} MB";
                }

                return "File not found";
            }
            catch
            {
                return "Error getting size";
            }
        }
    }
}
