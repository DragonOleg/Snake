using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Snake.Application.Interfaces;
using Snake.Core.Repositories.Command.Base;
using Snake.Core.Repositories.Query.Base;
using Snake.Persistence.Data;
using Snake.Persistence.Repository.Command.Base;
using Snake.Persistence.Repository.Query.Base;

namespace Snake.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection
            services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Snake");
            services.AddDbContext<SnakeDbContext>(options => { options.UseNpgsql(connectionString); });
            services.AddScoped<ISnakeDbContext>(provider => provider.GetService<SnakeDbContext>());
            services.AddScoped<IDbInitializer, DbInitializer>();
            services.AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));
            services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
            return services;
        }
    }
}