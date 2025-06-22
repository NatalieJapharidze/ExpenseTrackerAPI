using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.Database
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync();
    }
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {

            try
            {
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply database migrations.");
                throw;
            }
        }
    }
}
