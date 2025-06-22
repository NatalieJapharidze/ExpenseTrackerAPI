using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Reports
{
    public class SendEmailReport
    {
        public record SendEmailCommand(int UserId, int Year, int Month);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                SendEmailCommand command,
                IRepository<User> userRepository,
                IExcelService excelService,
                IEmailService emailService,
                ILogger<SendEmailReport> logger)
            {
                try
                {
                    if (command.UserId <= 0)
                        return Results.BadRequest(new { Message = "Invalid user ID" });

                    if (command.Year < 2000 || command.Year > DateTime.UtcNow.Year + 1)
                        return Results.BadRequest(new { Message = "Invalid year" });

                    if (command.Month < 1 || command.Month > 12)
                        return Results.BadRequest(new { Message = "Invalid month" });

                    var user = await userRepository.GetByIdAsync(command.UserId);
                    if (user == null)
                    {
                        logger.LogWarning("User {UserId} not found for email report", command.UserId);
                        return Results.NotFound(new { Message = "User not found" });
                    }

                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        logger.LogWarning("User {UserId} has no email address", command.UserId);
                        return Results.BadRequest(new { Message = "User has no email address configured" });
                    }

                    var startDate = new DateTime(command.Year, command.Month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    logger.LogInformation("Generating monthly report for user {UserId} for {Year}-{Month:D2}",
                        command.UserId, command.Year, command.Month);

                    var fileData = await excelService.GenerateExpenseReportAsync(
                        command.UserId,
                        startDate,
                        endDate);

                    if (fileData == null || fileData.Length == 0)
                    {
                        logger.LogWarning("Generated report is empty for user {UserId}", command.UserId);
                        return Results.BadRequest(new { Message = "No data available for the specified period" });
                    }

                    var fileName = $"Monthly_Expense_Report_{command.Year}_{command.Month:D2}.xlsx";
                    var subject = $"Monthly Expense Report - {startDate:MMMM yyyy}";

                    await emailService.SendMonthlyReportAsync(
                        user.Email,
                        user.FullName ?? user.Email,
                        subject,
                        fileData,
                        fileName,
                        startDate,
                        endDate);

                    logger.LogInformation("Monthly report email sent successfully to user {UserId} at {Email}",
                        command.UserId, user.Email);

                    return Results.Ok(new
                    {
                        Message = "Monthly report email sent successfully",
                        EmailSentTo = user.Email,
                        ReportPeriod = $"{startDate:MMMM yyyy}",
                        FileName = fileName,
                        SentAt = DateTime.UtcNow
                    });
                }
                catch (ArgumentException ex)
                {
                    logger.LogError(ex, "Invalid argument for email report generation: {Message}", ex.Message);
                    return Results.BadRequest(new { ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogError(ex, "Unauthorized access for user {UserId}", command.UserId);
                    return Results.Forbid();
                }
                catch (FileNotFoundException ex)
                {
                    logger.LogError(ex, "Required file not found for report generation: {Message}", ex.Message);
                    return Results.NotFound(new { Message = "Required resources not found" });
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Invalid operation during report generation: {Message}", ex.Message);
                    return Results.BadRequest(new { ex.Message });
                }
                catch (TimeoutException ex)
                {
                    logger.LogError(ex, "Timeout during report generation for user {UserId}", command.UserId);
                    return Results.Problem("Report generation is taking longer than expected. Please try again later.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error sending email report for user {UserId}: {Message}",
                        command.UserId, ex.Message);
                    return Results.Problem("An unexpected error occurred while sending the email report. Please try again later.");
                }
            }
        }
    }
}
