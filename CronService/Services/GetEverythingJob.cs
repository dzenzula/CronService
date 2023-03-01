using CronService.Data;
using CronService.Factories;
using CronService.Models;
using Quartz;

namespace CronService.Services
{
    public class GetEverythingJob : IJob
    {
        private readonly ILogger<GetEverythingJob> _logger;
        private readonly PgContext _pgContext;
        private readonly DevContext _devContext;
        private DateTimeOffset _fireTime;

        public GetEverythingJob(ILogger<GetEverythingJob> logger, DbFactory dbFactory)
        {
            _logger = logger;
            _devContext = dbFactory.CreateDevContext();
            _pgContext = dbFactory.CreatePgContext();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _fireTime = context.FireTimeUtc.ToOffset(TimeSpan.FromHours(2));
            var cron = context.JobDetail.Key.Name;
            var cronConfigs = _devContext.CronConfigs!.Select(x => x).Where(x => x.Cron == cron).ToList();

            foreach (var cronConfig in cronConfigs)
            {
                Console.WriteLine("job name: " + cron + " " + cronConfig.Type);
                GetValueAsync(cronConfig);
            }

            await _devContext.SaveChangesAsync();
        }

        private void GetValueAsync(CronConfig cronConfig)
        {
            try
            {
                var oldTime = GetOldTime(cronConfig);
                var frTb = cronConfig.FromTable!.Replace(" ", String.Empty);
                var toTb = cronConfig.ToTable!.Replace(" ", String.Empty);
                var type = cronConfig.Type!;

                switch (frTb + type)
                {
                    case "rawavg":
                    case "rawsum":
                        var groupAvgRaw = _pgContext.RawDataNumerics.Select(x => x)
                            .Where(x => x.Timestamp <= _fireTime && x.Timestamp >= oldTime && x.IdMeasuring == cronConfig.IdMeasuring)
                            .OrderByDescending(x => x.Timestamp)
                            .GroupBy(x => x.IdMeasuring);
                        GetFromRaw(groupAvgRaw, toTb, type);
                        break;

                    case "minutesavg":
                        var groupAvgMin = _devContext.AvgValuesMinutes!.Select(x => x)
                            .Where(x => x.Timestamp <= _fireTime && x.Timestamp >= oldTime && x.IdMeasuring == cronConfig.IdMeasuring)
                            .OrderByDescending(x => x.Timestamp)
                            .GroupBy(x => x.IdMeasuring);
                        GetFromMinutes(groupAvgMin, toTb);
                        break;

                    case "minutessum":
                        var groupSumMin = _devContext.SumValuesMinutes!.Select(x => x)
                            .Where(x => x.Timestamp <= _fireTime && x.Timestamp >= oldTime && x.IdMeasuring == cronConfig.IdMeasuring)
                            .OrderByDescending(x => x.Timestamp)
                            .GroupBy(x => x.IdMeasuring);
                        GetFromMinutes(groupSumMin, toTb);
                        break;

                    case "hoursavg":
                        var groupAvgHour = _devContext.AvgValuesHours!.Select(x => x)
                            .Where(x => x.Timestamp <= _fireTime && x.Timestamp >= oldTime && x.IdMeasuring == cronConfig.IdMeasuring)
                            .OrderByDescending(x => x.Timestamp)
                            .GroupBy(x => x.IdMeasuring);
                        GetFromHours(groupAvgHour);
                        break;

                    case "hourssum":
                        var groupSumHour = _devContext.SumValuesHours!.Select(x => x)
                            .Where(x => x.Timestamp <= _fireTime && x.Timestamp >= oldTime && x.IdMeasuring == cronConfig.IdMeasuring)
                            .OrderByDescending(x => x.Timestamp)
                            .GroupBy(x => x.IdMeasuring);
                        GetFromHours(groupSumHour);
                        break;

                    case "days":
                        break;
                }

                _logger.LogInformation("Job worked at: " + _fireTime.ToString() + " From: " + frTb + " To: " + toTb);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.Message);
            }
        }

        private DateTimeOffset GetOldTime(CronConfig cronConfig)
        {
            DateTimeOffset oldTime = DateTimeOffset.MinValue;
            var toTb = cronConfig.ToTable!.Replace(" ", String.Empty);
            var type = cronConfig.Type!;

            switch (toTb + type)
            {
                case "minutesavg":
                    oldTime = _devContext.AvgValuesMinutes!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();
                    if (oldTime == DateTimeOffset.MinValue)
                        oldTime = DateTimeOffset.UtcNow.AddMinutes(-1).ToOffset(TimeSpan.FromHours(2));
                    break;

                case "minutessum":
                    oldTime = _devContext.SumValuesMinutes!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();
                    if (oldTime == DateTimeOffset.MinValue)
                        oldTime = DateTimeOffset.UtcNow.AddMinutes(-1).ToOffset(TimeSpan.FromHours(2));
                    break;

                case "hoursavg":
                    oldTime = _devContext.AvgValuesHours!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();
                    if (oldTime == DateTimeOffset.MinValue)
                        oldTime = DateTimeOffset.UtcNow.AddHours(-1).ToOffset(TimeSpan.FromHours(2));
                    break;

                case "hourssum":
                    oldTime = _devContext.SumValuesHours!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();
                    if (oldTime == DateTimeOffset.MinValue)
                        oldTime = DateTimeOffset.UtcNow.AddHours(-1).ToOffset(TimeSpan.FromHours(2));
                    break;

                case "daysavg":
                    oldTime = _devContext.AvgValuesDays!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();
                    if (oldTime == DateTimeOffset.MinValue)
                        oldTime = DateTimeOffset.UtcNow.AddDays(-1).ToOffset(TimeSpan.FromHours(2));
                    break;

                case "dayssum":
                    oldTime = _devContext.SumValuesDays!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();
                    if (oldTime == DateTimeOffset.MinValue)
                        oldTime = DateTimeOffset.UtcNow.AddDays(-1).ToOffset(TimeSpan.FromHours(2));
                    break;
            }

            return oldTime;
        }

        private void GetFromRaw(IQueryable<IGrouping<int, RawDataNumeric>> groupRaw, string toTb, string type)
        {
            switch (toTb)
            {
                case "minutes":
                    GetAvgSumValueMinutes(groupRaw, type);
                    break;

                case "hours":
                    GetAvgSumValueHours(groupRaw, type);
                    break;

                case "days":
                    GetAvgSumValueDays(groupRaw, type);
                    break;
            }
        }

        private void GetFromMinutes(IQueryable<IGrouping<int, AvgValueMinutes>> groupMin, string toTb)
        {
            switch (toTb)
            {
                case "hours":
                    GetAvgValueHours(groupMin);
                    break;

                case "days":
                    GetAvgValueDays(groupMin);
                    break;
            }
        }

        private void GetFromMinutes(IQueryable<IGrouping<int, SumValueMinutes>> groupMin, string toTb)
        {
            switch (toTb)
            {
                case "hours":
                    GetSumValueHours(groupMin);
                    break;

                case "days":
                    GetSumValueDays(groupMin);
                    break;
            }
        }

        private void GetFromHours(IQueryable<IGrouping<int, AvgValueHours>> groupMin)
        {
            GetAvgValueDays(groupMin);
        }

        private void GetFromHours(IQueryable<IGrouping<int, SumValueHours>> groupMin)
        {
            GetSumValueDays(groupMin);
        }

        private void GetAvgSumValueMinutes(IQueryable<IGrouping<int, RawDataNumeric>> group, string type)
        {
            switch (type)
            {
                case "avg":
                    var avg = group.Select(x => new AvgValueMinutes
                    {
                        IdMeasuring = x.Key,
                        Timestamp = _fireTime,
                        Value = x.Average(x => x.Value),
                        Quality = (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count()
                    }).ToList();
                    _devContext.AddRange(avg);
                    break;

                case "sum":
                    var sum = group.Select(x => new SumValueMinutes
                    {
                        IdMeasuring = x.Key,
                        Timestamp = _fireTime,
                        Value = x.Sum(x => x.Value),
                        Quality = (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count()
                    }).ToList();
                    _devContext.AddRange(sum);
                    break;
            }
        }

        private void GetAvgSumValueHours(IQueryable<IGrouping<int, RawDataNumeric>> group, string type)
        {
            switch (type)
            {
                case "avg":
                    var avg = group.Select(x => new AvgValueHours
                    {
                        IdMeasuring = x.Key,
                        Timestamp = _fireTime,
                        Value = x.Average(x => x.Value),
                        Quality = (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count()
                    }).ToList();
                    _devContext.AddRange(avg);
                    break;

                case "sum":
                    var sum = group.Select(x => new SumValueHours
                    {
                        IdMeasuring = x.Key,
                        Timestamp = _fireTime,
                        Value = x.Sum(x => x.Value),
                        Quality = (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count()
                    }).ToList();
                    _devContext.AddRange(sum);
                    break;
            }
        }

        private void GetAvgSumValueDays(IQueryable<IGrouping<int, RawDataNumeric>> group, string type)
        {
            switch (type)
            {
                case "avg":
                    var avg = group.Select(x => new AvgValueDays
                    {
                        IdMeasuring = x.Key,
                        Timestamp = _fireTime,
                        Value = x.Average(x => x.Value),
                        Quality = (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count()
                    }).ToList();
                    _devContext.AddRange(avg);
                    break;

                case "sum":
                    var sum = group.Select(x => new SumValueDays
                    {
                        IdMeasuring = x.Key,
                        Timestamp = _fireTime,
                        Value = x.Sum(x => x.Value),
                        Quality = (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count()
                    }).ToList();
                    _devContext.AddRange(sum);
                    break;
            }
        }

        private void GetAvgValueHours(IQueryable<IGrouping<int, AvgValueMinutes>> group)
        {
            var avg = group.Select(x => new AvgValueHours
            {
                IdMeasuring = x.Key,
                Timestamp = _fireTime,
                Value = x.Average(x => x.Value),
                Quality = x.Average(x => x.Quality)
            }).ToList();
            _devContext.AddRange(avg);
        }

        private void GetSumValueHours(IQueryable<IGrouping<int, SumValueMinutes>> group)
        {
            var sum = group.Select(x => new SumValueHours
            {
                IdMeasuring = x.Key,
                Timestamp = _fireTime,
                Value = x.Sum(x => x.Value),
                Quality = x.Average(x => x.Quality)
            }).ToList();
            _devContext.AddRange(sum);
        }

        private void GetAvgValueDays(IQueryable<IGrouping<int, AvgValueMinutes>> group)
        {
            var avg = group.Select(x => new AvgValueDays
            {
                IdMeasuring = x.Key,
                Timestamp = _fireTime,
                Value = x.Average(x => x.Value),
                Quality = x.Average(x => x.Quality)
            }).ToList();
            _devContext.AddRange(avg);
        }

        private void GetAvgValueDays(IQueryable<IGrouping<int, AvgValueHours>> group)
        {
            var avg = group.Select(x => new AvgValueDays
            {
                IdMeasuring = x.Key,
                Timestamp = _fireTime,
                Value = x.Average(x => x.Value),
                Quality = x.Average(x => x.Quality)
            }).ToList();
            _devContext.AddRange(avg);
        }

        private void GetSumValueDays(IQueryable<IGrouping<int, SumValueMinutes>> group)
        {
            var sum = group.Select(x => new SumValueDays
            {
                IdMeasuring = x.Key,
                Timestamp = _fireTime,
                Value = x.Sum(x => x.Value),
                Quality = x.Average(x => x.Quality)
            }).ToList();
            _devContext.AddRange(sum);
        }

        private void GetSumValueDays(IQueryable<IGrouping<int, SumValueHours>> group)
        {
            var sum = group.Select(x => new SumValueDays
            {
                IdMeasuring = x.Key,
                Timestamp = _fireTime,
                Value = x.Sum(x => x.Value),
                Quality = x.Average(x => x.Quality)
            }).ToList();
            _devContext.AddRange(sum);
        }
    }
}