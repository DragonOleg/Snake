using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Snake.Application.Abstractions;
using Snake.Application.Interfaces;
using Snake.Core.Repositories.Command.Base;
using Snake.Core.Repositories.Query.Base;
using Snake.Persistence.Data;
using Snake.Persistence.Repositories;
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
            services.AddScoped<ISnakeDbContext>(provider => provider.GetRequiredService<SnakeDbContext>());
            services.AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));
            services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));

            // Рекорды: без scoped-состояния — DbContext создаётся на каждую операцию.
            services.AddSingleton(provider => new GameResultRepository(
                connectionString,
                provider.GetService<ILogger<GameResultRepository>>()));
            services.AddSingleton<IGameResultSink>(provider => provider.GetRequiredService<GameResultRepository>());
            services.AddSingleton<IGameResultReader>(provider => provider.GetRequiredService<GameResultRepository>());
            services.AddSingleton<IDbInitializer, DbInitializer>();

            return services;
        }
    }
}
