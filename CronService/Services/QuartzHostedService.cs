using CronService.Data;
using CronService.Factories;
using CronService.Models;
using Quartz;
using Quartz.Spi;

namespace CronService.Services
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly List<string?> _crons;

        public QuartzHostedService(ISchedulerFactory schedulerFactory, IJobFactory jobFactory, DbFactory dbFactory)
        {
            DevContext devContext = dbFactory.CreateDevContext();
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
            _crons = devContext.CronConfigs!.Select(x => x.Cron).Distinct().ToList();
        }

        public IScheduler? Scheduler { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
            List<JobSchedule> jobSchedules = new List<JobSchedule>();

            foreach (var cron in _crons)
            {
                jobSchedules.Add(new JobSchedule(jobType: typeof(GetEverythingJobTst), cronExpression: cron!));
            }

            foreach (var jobSchedule in jobSchedules)
            {
                var job = CreateJob(jobSchedule);
                var trigger = CreateTrigger(jobSchedule);

                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            }

            await Scheduler.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (File.Exists("TimeConfig.yaml"))
            {
                File.Delete("TimeConfig.yaml");
            }

            await Scheduler!.Shutdown(cancellationToken);
        }

        private static ITrigger CreateTrigger(JobSchedule schedule)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{schedule.JobType.FullName}.{schedule.CronExpression}.trigger")
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.CronExpression)
                .Build();
        }

        private static IJobDetail CreateJob(JobSchedule schedule)
        {
            var jobType = schedule.JobType;
            return JobBuilder
                .Create(jobType)
                .WithIdentity($"{schedule.CronExpression}")
                .WithDescription(jobType.Name)
                .Build();
        }
    }
}