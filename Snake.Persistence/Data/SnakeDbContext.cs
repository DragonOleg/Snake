using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Snake.Application.Interfaces;
using Snake.Persistence.Data.Entities;

namespace Snake.Persistence.Data
{
    public class SnakeDbContext : DbContext, ISnakeDbContext
    {
        public SnakeDbContext(DbContextOptions<SnakeDbContext> options)
            : base(options) { }

        public DbSet<GameResultEntity> GameResults { get; set; }

        public IDbContextTransaction BeginTransaction()
        {
            return this.Database.BeginTransaction();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<GameResultEntity>(entity =>
            {
                entity.ToTable("game_results");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(x => x.FinishedAtUtc)
                    .HasColumnName("finished_at_utc")
                    .HasColumnType("timestamp with time zone")
                    .IsRequired();
                entity.Property(x => x.Score).HasColumnName("score");
                entity.Property(x => x.Length).HasColumnName("length");
                entity.Property(x => x.Level).HasColumnName("level");
                entity.Property(x => x.Eaten).HasColumnName("eaten");
                entity.Property(x => x.DurationMs).HasColumnName("duration_ms");
                entity.Property(x => x.BoardWidth).HasColumnName("board_width");
                entity.Property(x => x.BoardHeight).HasColumnName("board_height");
                entity.Property(x => x.Outcome).HasColumnName("outcome").IsRequired();
                entity.Property(x => x.StatusMessage).HasColumnName("status_message");
            });
        }
    }
}
