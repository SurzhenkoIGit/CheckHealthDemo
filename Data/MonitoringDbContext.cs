using HealthCheckDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthCheckDemo.Data
{
    public class MonitoringDbContext : DbContext
    {
        public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : base(options) { }

        public DbSet<SiteError> Errors { get; set; }
        public DbSet<MonitoringMetric> Metrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteError>().HasIndex(e => e.Timestamp);
            modelBuilder.Entity<MonitoringMetric>().HasIndex(e => e.Timestamp);
        }
    }
}
