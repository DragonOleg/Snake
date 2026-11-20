using Microsoft.EntityFrameworkCore;
using Snake.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
namespace Snake.Persistence.Data
{
    public class SnakeDbContext : DbContext, ISnakeDbContext
    {
        public SnakeDbContext(DbContextOptions<SnakeDbContext> options)
            : base(options) { }
        
        public IDbContextTransaction BeginTransaction()
        {
            return this.Database.BeginTransaction();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}