using CronService.Interfaces;
using CronService.Models;
using Microsoft.EntityFrameworkCore;

namespace CronService.Helpers
{
    public static class DbSetHelper
    {
        public static IQueryable<T> Set<T>(this DbContext context, Type t) where T : IValue
        {
            if (context.Model.FindEntityType("CronService.Models." + t.Name) == null)
                return Enumerable.Empty<T>().AsQueryable();

            var method = context.GetType().GetMethod(nameof(DbContext.Set), types: Type.EmptyTypes)!.MakeGenericMethod(t);
            var dbSet = method.Invoke(context, null) as IQueryable<T>;

            return dbSet!;
        }

        public static IValue GetCalculation<T>(this IQueryable<T> table, CronConfig cronConfig,
            DateTimeOffset oldTime, DateTimeOffset fireTime) where T : IValue
        {
            RawDataNumeric? result = null;
            var frTb = cronConfig.FromTable!;
            var type = cronConfig.Type!;
            var toTb = cronConfig.ToTable!;
            var dbSetType = Type.GetType("CronService.Models." + toTb)!;

            try
            {
                do
                {
                    var entries = table.Select(x => x)
                        .Where(x => x.Timestamp < fireTime && x.Timestamp >= oldTime && x.IdMeasuring == cronConfig.IdMeasuring)
                        .OrderByDescending(x => x.Timestamp)
                        .AsEnumerable();

                    result = entries.GroupBy(x => x.IdMeasuring)
                        .Select(x => new RawDataNumeric
                        {
                            IdMeasuring = x.Key,
                            Timestamp = fireTime,
                            Value = x.CalculateValue(type),
                            Quality = x.CalculateQuality(frTb)
                        })
                        .FirstOrDefault();

                    if (result == null)
                    {
                        Thread.Sleep(60000);
                    }
                }
                while (result == null);

                var toTableClass = (IValue)Activator.CreateInstance(dbSetType)
                    ?? throw new ArgumentException($"Could not create instance of {dbSetType}");
                toTableClass.IdMeasuring = result.IdMeasuring;
                toTableClass.Timestamp = result.Timestamp;
                toTableClass.Value = result.Value;
                toTableClass.Quality = result.Quality;

                return toTableClass;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static decimal CalculateValue<T>(this IGrouping<int, T> x, string type) where T : IValue
        {
            if (type == "AVG")
                return x.Average(x => x.Value);
            else
                return x.Sum(x => x.Value);
        }

        private static decimal CalculateQuality<T>(this IGrouping<int, T> x, string frTb) where T : IValue
        {
            if (frTb == nameof(RawDataNumeric))
                return (decimal)x.Select(x => x).Count(x => x.Quality == 192) / x.Select(x => x.Quality).Count();
            else
                return x.Average(x => x.Quality);
        }
    }
}