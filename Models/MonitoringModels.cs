namespace HealthCheckDemo.Models
{
    public class SiteError
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public string StatusCode { get; set; }
        public long ResponseTime { get; set; }
    }

    public class MonitoringMetric
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string MetricName { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
    }
}

