namespace CronService.Models
{
    public class CronConfig
    {
        public int IdMeasuring { get; set; }
        public string? Type { get; set; }
        public string? Cron { get; set; }
        public string? FromTable { get; set; }
        public string? ToTable { get; set; }
        public string? Username { get; set; }
        public DateTimeOffset TimestampInsert { get; set; }
        public DateTimeOffset TimestampUpdate { get; set; }
    }
}