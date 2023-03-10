using CronService.Data;
using CronService.Factories;
using CronService.Helpers;
using CronService.Interfaces;
using CronService.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Quartz;

namespace CronService.Services
{
    public class GetEverythingJobTst : IJob
    {
        private readonly ILogger<GetEverythingJobTst> _logger;
        private readonly DbFactory _dbFactory;
        private DateTimeOffset _fireTime;
        private DevContext _devContext = new DevContext();

        public GetEverythingJobTst(ILogger<GetEverythingJobTst> logger, DbFactory dbFactory, IConfiguration configuration)
        {
            _logger = logger;
            _dbFactory = dbFactory;
            YamlHelper.Configure(configuration);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _devContext = _dbFactory.CreateDevContext();
            _fireTime = context.FireTimeUtc.ToOffset(TimeSpan.FromHours(2));
            string cron = context.JobDetail.Key.Name;
            List<CronConfig> cronConfigs = _devContext.CronConfigs!.Select(x => x).Where(x => x.Cron == cron).ToList();
            List<Task> tasks = new List<Task>();

            cronConfigs.ForEach(x => tasks.Add(GetCalculationValue(x)));

            await Task.WhenAll(tasks);

            _devContext.Dispose();
        }

        private async Task GetCalculationValue(CronConfig cronConfig)
        {
            try
            {
                DevContext devContext = _dbFactory.CreateDevContext();
                PgContext pgContext = _dbFactory.CreatePgContext();
                string frTb = cronConfig.FromTable!;
                string toTb = cronConfig.ToTable!;
                string key = cronConfig.IdMeasuring.ToString() + cronConfig.ToTable + cronConfig.Type;
                Type dbSetType = Type.GetType("CronService.Models." + frTb)!;

                DateTimeOffset oldTime = YamlHelper.GetOldTimeFromYAML(cronConfig);
                if (oldTime == DateTimeOffset.MinValue)
                {
                    oldTime = GetOldTime(cronConfig);
                    YamlHelper.WriteOldTimeToYAML(oldTime, key);
                }

                Console.WriteLine($"job name: {cronConfig.Cron} {cronConfig.Type}");

                IQueryable<IValue> dbSet = pgContext.Set<IValue>(dbSetType);
                if (!dbSet.Any())
                    dbSet = devContext.Set<IValue>(dbSetType);

                List<DateTimeOffset> missedTimes = GetMissedTimes(cronConfig, _fireTime, oldTime);
                await CalculateMissedValue(cronConfig, dbSet, missedTimes);

                YamlHelper.SaveYAML();

                _logger.LogInformation(1, message: $"Job worked at: {_fireTime} From: {frTb} To: {toTb}");

                devContext.Dispose();
                pgContext.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
                if (!File.Exists("logs.txt"))
                {
                    File.Create("logs.txt").Dispose();
                }
                using (var writer = new StreamWriter("logs.txt", true))
                {
                    await writer.WriteLineAsync($"Error: {_fireTime} {e.Message}");
                    writer.Close();
                }
            }
        }

        private async Task CalculateMissedValue(CronConfig cronConfig, IQueryable<IValue> dbSet, List<DateTimeOffset> missedTimes)
        {
            DateTimeOffset prevTime = DateTimeOffset.MinValue;

            foreach (var currTime in missedTimes)
            {
                using (DevContext db = _dbFactory.CreateDevContext())
                {
                    if (prevTime != DateTimeOffset.MinValue)
                    {
                        var res = dbSet.GetCalculation(cronConfig, prevTime, currTime);
                        if (res != null)
                        {
                            await db.AddAsync(res);
                            _logger.LogInformation(message: $"{currTime} is done, id: {cronConfig.IdMeasuring}");
                        }
                        else
                            _logger.LogWarning(message: $"{currTime} is null right now, id: {cronConfig.IdMeasuring}");
                    }
                    prevTime = currTime;
                    await db.SaveChangesAsync();
                }
            }
        }

        private DateTimeOffset GetOldTime(CronConfig cronConfig)
        {
            var toTb = cronConfig.ToTable!;
            var dbSetType = Type.GetType("CronService.Models." + toTb)!;

            DateTimeOffset oldTime = _devContext.Set<IValue>(dbSetType)!.Select(x => x.Timestamp).OrderByDescending(x => x).FirstOrDefault();

            if (oldTime == DateTimeOffset.MinValue)
            {
                if (toTb == nameof(AvgValueMinutes) || toTb == nameof(SumValueMinutes))
                    oldTime = _fireTime.AddMinutes(-1);
                else if (toTb == nameof(AvgValueHours) || toTb == nameof(SumValueHours))
                    oldTime = _fireTime.AddHours(-1);
                else
                    oldTime = _fireTime.AddDays(-1);
            }

            return oldTime;
        }

        private List<DateTimeOffset> GetMissedTimes(CronConfig cronConfig, DateTimeOffset fireTime, DateTimeOffset oldTime)
        {
            List<DateTimeOffset> missedTimes = new List<DateTimeOffset>();
            TimeSpan missedTime = fireTime - oldTime;
            int missedMinutes = (int)Math.Floor(missedTime.TotalMinutes) + 1;
            int missedHours = (int)Math.Floor(missedTime.TotalHours) + 1;
            int missedDays = (int)Math.Floor(missedTime.TotalDays) + 1;

            if (cronConfig.ToTable!.Contains("minutes", StringComparison.InvariantCultureIgnoreCase))
            {
                missedTimes = Enumerable.Range(0, missedMinutes).Select(n => oldTime.AddMinutes(n)).Where(n => n <= fireTime).ToList();
            }
            else if (cronConfig.ToTable.Contains("hours", StringComparison.InvariantCultureIgnoreCase))
            {
                missedTimes = Enumerable.Range(0, missedHours).Select(n => oldTime.AddHours(n)).Where(n => n <= fireTime).ToList();
            }
            else if (cronConfig.ToTable.Contains("days", StringComparison.InvariantCultureIgnoreCase))
            {
                missedTimes = Enumerable.Range(0, missedDays).Select(n => oldTime.AddDays(n)).Where(n => n <= fireTime).ToList();
            }

            return missedTimes;
        }
    }
}