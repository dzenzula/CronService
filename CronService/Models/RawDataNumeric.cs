using CronService.Interfaces;

namespace CronService.Models
{
    public partial class RawDataNumeric : IValue
    {
        public int IdMeasuring { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public decimal Value { get; set; }
        public decimal Quality { get; set; }
        public DateTimeOffset TimestampInsert { get; set; }

        public RawDataNumeric(int idMeasuring, DateTimeOffset timestamp, decimal value, decimal quality)
        {
            IdMeasuring = idMeasuring;
            Timestamp = timestamp;
            Value = value;
            Quality = quality;
        }
    }
}