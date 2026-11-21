using Microsoft.Extensions.Logging;
using Snake.Persistence.Repositories;
using System;

namespace Snake.Persistence
{
    public class DbInitializer : IDbInitializer
    {
        private readonly GameResultRepository _repository;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(GameResultRepository repository, ILogger<DbInitializer> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                _repository.EnsureSchema();
                _logger.LogInformation("Схема БД готова (game_results)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
