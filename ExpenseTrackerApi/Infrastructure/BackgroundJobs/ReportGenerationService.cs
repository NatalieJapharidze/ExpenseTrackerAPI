using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.BackgroundJobs
{
    public class ReportGenerationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportGenerationService> _logger;

        public ReportGenerationService(IServiceProvider serviceProvider, ILogger<ReportGenerationService> logger)
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
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var excelService = scope.ServiceProvider.GetRequiredService<IExcelService>();

                    var pendingJobs = await context.ReportJobs
                        .Where(j => j.Status == "pending")
                        .OrderBy(j => j.CreatedAt)
                        .Take(10)
                        .ToListAsync();

                    if (pendingJobs.Any())
                    {
                        _logger.LogInformation("Found {JobCount} pending report jobs", pendingJobs.Count);
                    }

                    foreach (var job in pendingJobs)
                    {
                        await ProcessReportJob(context, excelService, job);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in report generation service");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessReportJob(AppDbContext context, IExcelService excelService, ExpenseTrackerApi.Infrastructure.Database.Entities.ReportJob job)
        {
            try
            {
                _logger.LogInformation("Processing report job {JobId} for user {UserId}", job.Id, job.UserId);

                job.Status = "processing";
                await context.SaveChangesAsync();

                DateTime startDate, endDate;
                CalculateDateRange(job.ReportType, out startDate, out endDate);

                var fileData = await excelService.GenerateExpenseReportAsync(
                    job.UserId,
                    startDate,
                    endDate);

                if (fileData == null || fileData.Length == 0)
                {
                    throw new InvalidOperationException("Generated report is empty");
                }

                var fileName = $"report_{job.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                var fileUrl = await SaveReportFile(fileData, fileName);

                job.FileUrl = fileUrl;
                job.Status = "completed";
                job.CompletedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("Report job {JobId} completed successfully. File saved at {FileUrl}",
                    job.Id, fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report for job {JobId}: {Message}", job.Id, ex.Message);

                try
                {
                    job.Status = "failed";
                    job.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to update job {JobId} status to failed", job.Id);
                }
            }
        }

        private static void CalculateDateRange(string reportType, out DateTime startDate, out DateTime endDate)
        {
            var now = DateTime.UtcNow;

            switch (reportType?.ToLower())
            {
                case "monthly":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;

                case "yearly":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = new DateTime(now.Year, 12, 31);
                    break;

                case "quarterly":
                    var quarter = (now.Month - 1) / 3 + 1;
                    startDate = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    endDate = startDate.AddMonths(3).AddDays(-1);
                    break;

                case "weekly":
                    var daysSinceMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
                    startDate = now.Date.AddDays(-daysSinceMonday);
                    endDate = startDate.AddDays(6);
                    break;

                default:
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
            }
        }

        private async Task<string> SaveReportFile(byte[] fileData, string fileName)
        {
            try
            {
                var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reports");
                Directory.CreateDirectory(reportsDirectory);

                var filePath = Path.Combine(reportsDirectory, fileName);
                await File.WriteAllBytesAsync(filePath, fileData);

                var fileUrl = $"/reports/{fileName}";

                _logger.LogInformation("Report file saved successfully: {FilePath}", filePath);

                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save report file {FileName}: {Message}", fileName, ex.Message);
                throw new InvalidOperationException($"Failed to save report file: {ex.Message}", ex);
            }
        }
    }
}
