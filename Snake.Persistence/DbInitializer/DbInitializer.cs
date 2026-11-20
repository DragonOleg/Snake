using Microsoft.Extensions.Logging;
using System;

namespace Snake.Persistence
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(ILogger<DbInitializer> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                Fill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        private void Fill()
        {
        }
    }
}