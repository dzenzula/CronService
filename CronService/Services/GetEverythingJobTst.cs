using CronService.Data;
using CronService.Factories;
using CronService.Helpers;
using CronService.Interfaces;
using CronService.Models;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace CronService.Services
{
    public class GetEverythingJobTst : IJob
    {
        private readonly ILogger<GetEverythingJobTst> _logger;
        private readonly PgContext _pgContext;
        private readonly DevContext _devContext;
        private readonly DbFactory _dbFactory;
        private DateTimeOffset _fireTime;

        public GetEverythingJobTst(ILogger<GetEverythingJobTst> logger, DbFactory dbFactory, IConfiguration configuration)
        {
            _logger = logger;
            _devContext = dbFactory.CreateDevContext();
            _pgContext = dbFactory.CreatePgContext();
            _dbFactory = dbFactory;
            YamlHelper.Configure(configuration);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _fireTime = context.FireTimeUtc.ToOffset(TimeSpan.FromHours(2));
            string cron = context.JobDetail.Key.Name;
            List<CronConfig> cronConfigs = _devContext.CronConfigs!.Select(x => x).Where(x => x.Cron == cron).ToList();
            List<Task> tasks = new List<Task>();

            cronConfigs.ForEach(x => tasks.Add(GetCalculationValue(x)));

            await Task.WhenAll(tasks);
            await _devContext.SaveChangesAsync();

            YamlHelper.SaveYAML();

            _devContext.Dispose();
            _pgContext.Dispose();
        }

        private async Task GetCalculationValue(CronConfig cronConfig)
        {
            try
            {
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

                IQueryable<IValue> dbSet = _pgContext.Set<IValue>(dbSetType);
                if (!dbSet.Any())
                    dbSet = _devContext.Set<IValue>(dbSetType);

                List<DateTimeOffset> missedTimes = GetMissedTimes(cronConfig, _fireTime, oldTime);
                if (missedTimes.Count > 1)
                    CalculateMissedValue(cronConfig, dbSet, missedTimes);

                var result = dbSet.GetCalculation(cronConfig, oldTime, _fireTime);

                YamlHelper.UpdateOldTimeYAML(_fireTime, key);

                await _devContext.AddAsync(result);

                _logger.LogInformation(message: $"Job worked at: {_fireTime} From: {frTb} To: {toTb}");
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

        private void CalculateMissedValue(CronConfig cronConfig, IQueryable<IValue> dbSet, List<DateTimeOffset> missedTimes)
        {
            DateTimeOffset prevTime = DateTimeOffset.MinValue;

            missedTimes.ForEach(currTime =>
            {
                using (DevContext db = _dbFactory.CreateDevContext())
                {
                    if (prevTime != DateTimeOffset.MinValue)
                    {
                        var tst = dbSet.GetCalculation(cronConfig, prevTime, currTime);
                        db.Add(tst);
                        db.SaveChanges();
                    }
                    prevTime = currTime;
                }
            });
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
            int missedMinutes = (int)Math.Floor(missedTime.TotalMinutes);
            int missedHours = (int)Math.Floor(missedTime.TotalHours);
            int missedDays = (int)Math.Floor(missedTime.TotalDays);

            if (cronConfig.ToTable.Contains("minutes", StringComparison.InvariantCultureIgnoreCase))
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