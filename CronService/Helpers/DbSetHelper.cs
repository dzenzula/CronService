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
            DateTimeOffset prevTime, DateTimeOffset currTime) where T : IValue
        {
            var frTb = cronConfig.FromTable!;
            var type = cronConfig.Type!;
            var toTb = cronConfig.ToTable!;
            string key = cronConfig.IdMeasuring.ToString() + cronConfig.ToTable + cronConfig.Type;
            var dbSetType = Type.GetType("CronService.Models." + toTb)!;
            var entries = table.Select(x => x)
                .Where(x => x.Timestamp < currTime && x.Timestamp >= prevTime && x.IdMeasuring == cronConfig.IdMeasuring)
                .OrderByDescending(x => x.Timestamp)
                .AsEnumerable();

            var result = entries.GroupBy(x => x.IdMeasuring)
                .Select(x => new RawDataNumeric
                (
                    x.Key,
                    currTime,
                    x.CalculateValue(type),
                    x.CalculateQuality(frTb)
                ))
                .FirstOrDefault();

            if (result != null)
            {
                YamlHelper.UpdateOldTimeYAML(currTime, key);
            }
            else
            {
                if (!File.Exists("logsnull.txt"))
                {
                    File.Create("logsnull.txt").Dispose();
                }
                using (var writer = new StreamWriter("logsnull.txt", true))
                {
                    writer.WriteLine($"Error: {currTime}, id: {cronConfig.IdMeasuring} was null");
                    writer.Close();
                }
                return null;
            }

            var toTableClass = (IValue)Activator.CreateInstance(dbSetType)
                ?? throw new ArgumentException($"Could not create instance of {dbSetType}");
            toTableClass.IdMeasuring = result.IdMeasuring;
            toTableClass.Timestamp = result.Timestamp;
            toTableClass.Value = result.Value;
            toTableClass.Quality = result.Quality;

            return toTableClass;
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