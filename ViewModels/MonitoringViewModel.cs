using HealthCheckDemo.Models;
using System.ComponentModel;

namespace HealthCheckDemo.ViewModels
{
    public class MonitoringViewModel
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public List<SiteError> Errors { get; set; }
        public List<MonitoringMetric> Metrics { get; set; }
    }
}
