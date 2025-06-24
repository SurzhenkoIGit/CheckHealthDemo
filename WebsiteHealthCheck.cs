using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

public class WebsiteHealthCheck : IHealthCheck
{
    private HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebsiteHealthCheck> _logger;
    public WebsiteHealthCheck(IConfiguration configuration, ILogger<WebsiteHealthCheck> logger)
    {
        _httpClient = new HttpClient();
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = _configuration["HealthCheckSettings:Url"];
            var maxResponseTime = int.Parse(_configuration["HealthCheckSettings:MaxResponseTime"]);

            var watch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url, cancellationToken);
            watch.Stop();

            var responseTime = watch.ElapsedMilliseconds;

            var data = new Dictionary<string, object>
            {
                { "ResponseTime", responseTime },
                { "Status", response.StatusCode.ToString() },
                { "URL", url }
            };

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Health check failed: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Unhealthy($"API returned {response.StatusCode}",
                    data: data);
            }

            if (responseTime > maxResponseTime)
            {
                _logger.LogWarning("Rresponse time exceeded: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded($"Response time is {responseTime}ms > {responseTime}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy("API is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return HealthCheckResult.Unhealthy("Health check failed", exception: ex);
        }
    }
}