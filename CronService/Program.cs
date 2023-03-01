using CronService.Factories;
using CronService.Services;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(services =>
    {
        services.AddTransient<DbFactory>();

        services.AddSingleton<IJobFactory, JobFactory>();
        services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
        services.AddHostedService<QuartzHostedService>();

        services.AddTransient<GetEverythingJobTst>();
    })
    .Build();

await host.RunAsync();