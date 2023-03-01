using CronService.Data;
using Microsoft.EntityFrameworkCore;

namespace CronService.Factories
{
    public class DbFactory
    {
        private readonly string _devConString;
        private readonly string _pgConString;

        public DbFactory(IConfiguration configuration)
        {
            _devConString = configuration.GetConnectionString("DevConString")!;
            _pgConString = configuration.GetConnectionString("PgConString")!;
        }

        public DevContext CreateDevContext()
        {
            return new DevContext(new DbContextOptionsBuilder<DevContext>().EnableSensitiveDataLogging().UseSqlServer(_devConString).Options);
        }

        public PgContext CreatePgContext()
        {
            return new PgContext(new DbContextOptionsBuilder<PgContext>().EnableSensitiveDataLogging().UseNpgsql(_pgConString).Options);
        }
    }
}