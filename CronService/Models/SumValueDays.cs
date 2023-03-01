using CronService.Interfaces;

namespace CronService.Models
{
    public class SumValueDays : IValue
    {
        public int IdMeasuring { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public decimal Value { get; set; }
        public decimal Quality { get; set; }
    }
}