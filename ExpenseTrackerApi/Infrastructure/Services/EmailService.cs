using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace ExpenseTrackerApi.Infrastructure.Services
{
    public interface IEmailService
    {
        Task SendMonthlyReportAsync(string recipientEmail, string recipientName, string subject, byte[] attachmentData, 
                                    string attachmentFileName, DateTime reportStartDate, DateTime reportEndDate); 
        Task SendBudgetAlertAsync(string recipientEmail, string categoryName, decimal currentAmount, decimal budgetAmount, decimal percentage); 
        Task SendNotificationAsync(string recipientEmail, string subject, string message);    
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendMonthlyReportAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            byte[] attachmentData,
            string attachmentFileName,
            DateTime reportStartDate,
            DateTime reportEndDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipientEmail))
                    throw new ArgumentException("Recipient email cannot be empty", nameof(recipientEmail));

                if (attachmentData == null || attachmentData.Length == 0)
                    throw new ArgumentException("Attachment data cannot be empty", nameof(attachmentData));

                using var smtpClient = CreateSmtpClient();
                using var mailMessage = new MailMessage();

                mailMessage.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                mailMessage.To.Add(new MailAddress(recipientEmail, recipientName));
                mailMessage.Subject = subject;
                mailMessage.IsBodyHtml = true;

                var emailBody = GenerateMonthlyReportEmailBody(recipientName, reportStartDate, reportEndDate);
                mailMessage.Body = emailBody;

                using var attachmentStream = new MemoryStream(attachmentData);
                var attachment = new Attachment(attachmentStream, attachmentFileName,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                mailMessage.Attachments.Add(attachment);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Monthly report email sent successfully to {Email} for period {StartDate} to {EndDate}",
                    recipientEmail, reportStartDate.ToString("yyyy-MM-dd"), reportEndDate.ToString("yyyy-MM-dd"));
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending monthly report to {Email}: {Message}", recipientEmail, ex.Message);
                throw new InvalidOperationException($"Failed to send email due to SMTP error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending monthly report to {Email}: {Message}", recipientEmail, ex.Message);
                throw;
            }
        }

        public async Task SendBudgetAlertAsync(
            string recipientEmail,
            string categoryName,
            decimal currentAmount,
            decimal budgetAmount,
            decimal percentage)
        {
            try
            {
                using var smtpClient = CreateSmtpClient();
                using var mailMessage = new MailMessage();

                mailMessage.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                mailMessage.To.Add(recipientEmail);
                mailMessage.Subject = $"Budget Alert: {categoryName} - {percentage:F1}% Used";
                mailMessage.IsBodyHtml = true;

                var emailBody = GenerateBudgetAlertEmailBody(categoryName, currentAmount, budgetAmount, percentage);
                mailMessage.Body = emailBody;

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Budget alert sent to {Email} for category {Category}", recipientEmail, categoryName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending budget alert to {Email}: {Message}", recipientEmail, ex.Message);
                throw;
            }
        }

        public async Task SendNotificationAsync(string recipientEmail, string subject, string message)
        {
            try
            {
                using var smtpClient = CreateSmtpClient();
                using var mailMessage = new MailMessage();

                mailMessage.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                mailMessage.To.Add(recipientEmail);
                mailMessage.Subject = subject;
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = false;

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Notification email sent to {Email}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to {Email}: {Message}", recipientEmail, ex.Message);
                throw;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            return smtpClient;
        }

        private static string GenerateMonthlyReportEmailBody(string recipientName, DateTime startDate, DateTime endDate)
        {
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html>");
            body.AppendLine("<html><head><meta charset='utf-8'></head><body>");
            body.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");

            body.AppendLine($"<h2 style='color: #2c3e50;'>Monthly Expense Report</h2>");
            body.AppendLine($"<p>Dear {recipientName},</p>");
            body.AppendLine($"<p>Please find attached your monthly expense report for the period:</p>");
            body.AppendLine($"<p><strong>{startDate:MMMM d, yyyy} - {endDate:MMMM d, yyyy}</strong></p>");

            body.AppendLine("<p>The attached Excel file contains:</p>");
            body.AppendLine("<ul>");
            body.AppendLine("<li>Detailed expense breakdown by category</li>");
            body.AppendLine("<li>Monthly spending trends</li>");
            body.AppendLine("<li>Budget comparison analysis</li>");
            body.AppendLine("<li>Summary statistics</li>");
            body.AppendLine("</ul>");

            body.AppendLine("<p>If you have any questions about this report, please don't hesitate to contact us.</p>");
            body.AppendLine("<p>Best regards,<br>Expense Tracker Team</p>");
            body.AppendLine("</div>");
            body.AppendLine("</body></html>");

            return body.ToString();
        }

        private static string GenerateBudgetAlertEmailBody(string categoryName, decimal currentAmount, decimal budgetAmount, decimal percentage)
        {
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html>");
            body.AppendLine("<html><head><meta charset='utf-8'></head><body>");
            body.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");

            body.AppendLine($"<h2 style='color: #e74c3c;'>Budget Alert: {categoryName}</h2>");
            body.AppendLine($"<p>You have used <strong>{percentage:F1}%</strong> of your monthly budget for <strong>{categoryName}</strong>.</p>");
            body.AppendLine($"<p>Current spending: <strong>${currentAmount:F2}</strong></p>");
            body.AppendLine($"<p>Monthly budget: <strong>${budgetAmount:F2}</strong></p>");
            body.AppendLine($"<p>Remaining budget: <strong>${budgetAmount - currentAmount:F2}</strong></p>");

            if (percentage >= 80)
            {
                body.AppendLine("<p style='color: #e74c3c; font-weight: bold;'>⚠️ Consider reviewing your spending in this category to stay within budget.</p>");
            }

            body.AppendLine("<p>Best regards,<br>Expense Tracker Team</p>");
            body.AppendLine("</div>");
            body.AppendLine("</body></html>");

            return body.ToString();
        }
    }
}
