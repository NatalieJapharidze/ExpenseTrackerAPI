using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Reports
{
    public class GenerateExcelReport
    {
        public record GenerateReportCommand(int UserId, DateTime StartDate, DateTime EndDate, string ReportType);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                GenerateReportCommand command,
                IExcelService excelService,
                ILogger<GenerateExcelReport> logger)
            {
                try
                {
                    if (command.UserId <= 0)
                        return Results.BadRequest(new { Message = "Invalid user ID" });

                    if (command.StartDate > command.EndDate)
                        return Results.BadRequest(new { Message = "Start date cannot be after end date" });

                    if (command.EndDate > DateTime.UtcNow.AddDays(1))
                        return Results.BadRequest(new { Message = "End date cannot be in the future" });

                    var maxDateRange = TimeSpan.FromDays(365);
                    if (command.EndDate - command.StartDate > maxDateRange)
                        return Results.BadRequest(new { Message = "Date range cannot exceed 365 days" });

                    logger.LogInformation("Generating Excel report for user {UserId} from {StartDate} to {EndDate}",
                        command.UserId, command.StartDate.ToString("yyyy-MM-dd"), command.EndDate.ToString("yyyy-MM-dd"));

                    var fileData = await excelService.GenerateExpenseReportAsync(
                        command.UserId,
                        command.StartDate,
                        command.EndDate);

                    if (fileData == null || fileData.Length == 0)
                    {
                        logger.LogWarning("Generated report is empty for user {UserId}", command.UserId);
                        return Results.BadRequest(new { Message = "No data available for the specified period" });
                    }

                    var fileName = $"expense_report_{command.StartDate:yyyyMMdd}_{command.EndDate:yyyyMMdd}.xlsx";

                    logger.LogInformation("Excel report generated successfully for user {UserId}, file size: {FileSize} bytes",
                        command.UserId, fileData.Length);

                    return Results.File(
                        fileData,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
                catch (ArgumentException ex)
                {
                    logger.LogError(ex, "Invalid argument for report generation: {Message}", ex.Message);
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogError(ex, "Unauthorized access for user {UserId}", command.UserId);
                    return Results.Forbid();
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Invalid operation during report generation: {Message}", ex.Message);
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (TimeoutException ex)
                {
                    logger.LogError(ex, "Timeout during report generation for user {UserId}", command.UserId);
                    return Results.Problem("Report generation is taking longer than expected. Please try again later.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error generating Excel report for user {UserId}: {Message}",
                        command.UserId, ex.Message);
                    return Results.Problem("An unexpected error occurred while generating the report. Please try again later.");
                }
            }
        }
    }
}
