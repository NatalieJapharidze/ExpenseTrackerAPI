using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.BackgroundJobs
{
    public class BudgetAlertService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BudgetAlertService> _logger;

        public BudgetAlertService(IServiceProvider serviceProvider, ILogger<BudgetAlertService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting budget alert check");

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await CheckBudgetAlerts(context, emailService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in budget alert service");
                }

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task CheckBudgetAlerts(AppDbContext context, IEmailService emailService)
        {
            var currentDate = DateTime.UtcNow;
            var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var currentMonth = currentDate.ToString("yyyy-MM");

            _logger.LogInformation("Checking budget alerts for month: {Month}", currentMonth);

            var categories = await context.Categories
                .Include(c => c.User)
                .Where(c => c.IsActive && c.MonthlyBudget > 0)
                .ToListAsync();

            _logger.LogInformation("Found {Count} categories with budgets", categories.Count);

            foreach (var category in categories)
            {
                try
                {
                    var monthlySpent = await context.Expenses
                        .Where(e => e.CategoryId == category.Id &&
                                   e.ExpenseDate.Date >= startOfMonth.Date &&
                                   e.ExpenseDate.Date <= endOfMonth.Date)
                        .SumAsync(e => e.Amount);

                    var percentage = category.MonthlyBudget > 0
                        ? (monthlySpent / category.MonthlyBudget) * 100
                        : 0;

                    _logger.LogInformation("Category {CategoryName}: Spent {Spent:C} of {Budget:C} ({Percentage:F1}%)",
                        category.Name, monthlySpent, category.MonthlyBudget, percentage);

                    if (percentage >= 80)
                    {
                        var alertExists = await context.BudgetAlerts
                            .AnyAsync(a => a.CategoryId == category.Id &&
                                          a.Month == currentMonth &&
                                          a.PercentageUsed >= 80);

                        if (!alertExists)
                        {
                            _logger.LogInformation("Sending budget alert for category {CategoryName} at {Percentage:F1}%",
                                category.Name, percentage);

                            try
                            {
                                await emailService.SendBudgetAlertAsync(
                                    category.User.Email,
                                    category.Name,
                                    monthlySpent,
                                    category.MonthlyBudget,
                                    percentage);

                                var alert = new BudgetAlert
                                {
                                    UserId = category.UserId,
                                    CategoryId = category.Id,
                                    Month = currentMonth,
                                    PercentageUsed = percentage,
                                    AlertSentAt = DateTime.UtcNow
                                };

                                context.BudgetAlerts.Add(alert);
                                await context.SaveChangesAsync();

                                _logger.LogInformation("Budget alert sent and recorded for {Email} - {CategoryName}",
                                    category.User.Email, category.Name);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Failed to send budget alert email for category {CategoryName}",
                                    category.Name);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Budget alert already sent this month for category {CategoryName}",
                                category.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing budget alert for category {CategoryName}", category.Name);
                }
            }

            _logger.LogInformation("Budget alert check completed");
        }
    }
}
