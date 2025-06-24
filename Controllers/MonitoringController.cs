using HealthCheckDemo.Data;
using HealthCheckDemo.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCheckDemo.Controllers
{
    public class MonitoringController : Controller
    {
        private readonly MonitoringDbContext _context;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(MonitoringDbContext context, ILogger<MonitoringController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            from ??= DateTime.UtcNow.AddDays(-7);
            to ??= DateTime.UtcNow;

            var errors = await _context.Errors.Where(e => e.Timestamp >= from && e.Timestamp <= to).OrderByDescending(e => e.Timestamp).ToListAsync();

            var metrics = await _context.Metrics.Where(m => m.Timestamp >= from && m.Timestamp <= to).OrderByDescending(m => m.Timestamp).ToListAsync();

            return View(new MonitoringViewModel
            {
                From = from.Value,
                To = to.Value,
                Errors = errors,
                Metrics = metrics
            });
        }
    }
}
