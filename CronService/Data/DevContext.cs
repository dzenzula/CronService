using CronService.Models;
using Microsoft.EntityFrameworkCore;

namespace CronService.Data
{
    public class DevContext : DbContext
    {
        public DevContext()
        {
        }

        public DevContext(DbContextOptions<DevContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AvgValueMinutes>? AvgValuesMinutes { get; set; }
        public virtual DbSet<AvgValueHours>? AvgValuesHours { get; set; }
        public virtual DbSet<AvgValueDays>? AvgValuesDays { get; set; }
        public virtual DbSet<SumValueMinutes>? SumValuesMinutes { get; set; }
        public virtual DbSet<SumValueHours>? SumValuesHours { get; set; }
        public virtual DbSet<SumValueDays>? SumValuesDays { get; set; }
        public virtual DbSet<CronConfig>? CronConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AvgValueMinutes>(entity =>
            {
                entity.ToTable(nameof(AvgValueMinutes));

                entity.HasKey(x => x.IdMeasuring).HasName("PK_AvgValueMinutes");

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Timestamp).HasColumnName("Timestamp");
                entity.Property(x => x.Value).HasColumnName("AvgValue");
                entity.Property(x => x.Quality).HasColumnName("Quality");
            });

            modelBuilder.Entity<AvgValueHours>(entity =>
            {
                entity.ToTable(nameof(AvgValueHours));

                entity.HasKey(x => x.IdMeasuring).HasName("PK_AvgValueHours");

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Timestamp).HasColumnName("Timestamp");
                entity.Property(x => x.Value).HasColumnName("AvgValue");
                entity.Property(x => x.Quality).HasColumnName("Quality");
            });

            modelBuilder.Entity<AvgValueDays>(entity =>
            {
                entity.ToTable(nameof(AvgValueDays));

                entity.HasKey(x => x.IdMeasuring).HasName("PK_AvgValueDays");

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Timestamp).HasColumnName("Timestamp");
                entity.Property(x => x.Value).HasColumnName("AvgValue");
                entity.Property(x => x.Quality).HasColumnName("Quality");
            });

            modelBuilder.Entity<SumValueMinutes>(entity =>
            {
                entity.ToTable(nameof(SumValueMinutes));

                entity.HasKey(x => x.IdMeasuring).HasName("PK_SumValueMinutes");

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Timestamp).HasColumnName("Timestamp");
                entity.Property(x => x.Value).HasColumnName("SumValue");
                entity.Property(x => x.Quality).HasColumnName("Quality");
            });

            modelBuilder.Entity<SumValueHours>(entity =>
            {
                entity.ToTable(nameof(SumValueHours));

                entity.HasKey(x => x.IdMeasuring).HasName("PK_SumValueHours");

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Timestamp).HasColumnName("Timestamp");
                entity.Property(x => x.Value).HasColumnName("SumValue");
                entity.Property(x => x.Quality).HasColumnName("Quality");
            });

            modelBuilder.Entity<SumValueDays>(entity =>
            {
                entity.ToTable(nameof(SumValueDays));

                entity.HasKey(x => x.IdMeasuring).HasName("PK_SumValueDays");

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Timestamp).HasColumnName("Timestamp");
                entity.Property(x => x.Value).HasColumnName("SumValue");
                entity.Property(x => x.Quality).HasColumnName("Quality");
            });

            modelBuilder.Entity<CronConfig>(entity =>
            {
                entity.ToTable(nameof(CronConfig));

                entity.HasNoKey();

                entity.Property(x => x.IdMeasuring).HasColumnName("IdMeasuring");
                entity.Property(x => x.Type).HasColumnName("Type");
                entity.Property(x => x.Cron).HasColumnName("Cron");
                entity.Property(x => x.FromTable).HasColumnName("FromTable");
                entity.Property(x => x.ToTable).HasColumnName("ToTable");
                entity.Property(x => x.Username).HasColumnName("Username");
                entity.Property(x => x.TimestampInsert).HasColumnName("TimestampInsert");
                entity.Property(x => x.TimestampUpdate).HasColumnName("TimestampUpdate");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}