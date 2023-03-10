using CronService.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CronService.Data
{
    public partial class PgContext : DbContext
    {
        public PgContext()
        {
        }

        public PgContext(DbContextOptions<PgContext> options)
            : base(options)
        {
        }

        public virtual DbSet<RawDataNumeric> RawDataNumerics { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            //if (!optionsBuilder.IsConfigured)
            //{
            //    optionsBuilder.UseNpgsql("Server=krr-sql-pgnode01;Database=krr-pa-raw-data;Username=aadzenzura;Password=h)#X2Ubu04Qm;");
            //}
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum("datatype", new[] { "digital", "string" })
                .HasPostgresExtension("tds_fdw")
                .HasPostgresExtension("timescaledb");

            modelBuilder.Entity<RawDataNumeric>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("raw_data_numeric");

                entity.HasIndex(e => e.Timestamp, "data_raw_numeric_timestamp_idx")
                    .HasSortOrder(new[] { SortOrder.Descending });

                entity.Property(e => e.IdMeasuring).HasColumnName("id_measuring");

                entity.Property(e => e.Quality).HasPrecision(4, 0).HasColumnName("quality");

                entity.Property(e => e.Timestamp).HasColumnName("timestamp");

                entity.Property(e => e.TimestampInsert)
                    .HasColumnName("timestamp_insert")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Value)
                    .HasPrecision(18, 6)
                    .HasColumnName("value");
            });

            modelBuilder.HasSequence("chunk_constraint_name", "_timescaledb_catalog");

            modelBuilder.HasSequence("chunk_copy_operation_id_seq", "_timescaledb_catalog");

            modelBuilder.HasSequence<int>("event_threshold_types_id_seq", "event");
        }
    }
}