using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.BackgroundJobs
{
    public class MonthlyEmailService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyEmailService> _logger;

        public MonthlyEmailService(IServiceProvider serviceProvider, ILogger<MonthlyEmailService> logger)
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
                    var now = DateTime.UtcNow;

                    if (now.Day == 1 && now.Hour == 9)
                    {
                        _logger.LogInformation("Starting monthly email report generation for {Date}", now.ToString("yyyy-MM-dd"));

                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var excelService = scope.ServiceProvider.GetRequiredService<IExcelService>();

                        await SendMonthlyReports(context, emailService, excelService, now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monthly email service");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task SendMonthlyReports(AppDbContext context, IEmailService emailService, IExcelService excelService, DateTime currentDate)
        {
            var lastMonth = currentDate.AddMonths(-1);
            var startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            _logger.LogInformation("Generating monthly reports for period {StartDate} to {EndDate}",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var users = await context.Users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            _logger.LogInformation("Found {UserCount} users with email addresses", users.Count);

            var successCount = 0;
            var failureCount = 0;

            foreach (var user in users)
            {
                try
                {
                    var hasExpenses = await context.Expenses
                        .AnyAsync(e => e.UserId == user.Id &&
                                      e.ExpenseDate >= startDate &&
                                      e.ExpenseDate <= endDate);

                    if (!hasExpenses)
                    {
                        _logger.LogInformation("User {UserId} has no expenses for {Month}, skipping report",
                            user.Id, lastMonth.ToString("yyyy-MM"));
                        continue;
                    }

                    var fileData = await excelService.GenerateExpenseReportAsync(
                        user.Id,
                        startDate,
                        endDate);

                    if (fileData == null || fileData.Length == 0)
                    {
                        _logger.LogWarning("Generated report is empty for user {UserId}", user.Id);
                        continue;
                    }

                    var fileName = $"Monthly_Expense_Report_{lastMonth:yyyy_MM}.xlsx";
                    var subject = $"Monthly Expense Report - {lastMonth:MMMM yyyy}";

                    await emailService.SendMonthlyReportAsync(
                        user.Email,
                        user.FullName ?? user.Email,
                        subject,
                        fileData,
                        fileName,
                        startDate,
                        endDate);

                    successCount++;
                    _logger.LogInformation("Monthly report sent successfully to {Email} for user {UserId}",
                        user.Email, user.Id);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "Failed to send monthly report to {Email} for user {UserId}: {Message}",
                        user.Email, user.Id, ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            _logger.LogInformation("Monthly email report generation completed. Success: {SuccessCount}, Failures: {FailureCount}",
                successCount, failureCount);
        }
    }
}
