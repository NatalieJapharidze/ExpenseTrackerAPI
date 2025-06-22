using System.Drawing;
using ExpenseTrackerApi.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ExpenseTrackerApi.Infrastructure.Services
{
    public interface IExcelService
    {
        Task<byte[]> GenerateExpenseReportAsync(int userId, DateTime startDate, DateTime endDate);
    }

    public class ExcelService : IExcelService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(AppDbContext context, ILogger<ExcelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> GenerateExpenseReportAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating expense report for user {UserId} from {StartDate} to {EndDate}",
                    userId, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.User)
                    .Where(e => e.UserId == userId &&
                               e.ExpenseDate >= startDate &&
                               e.ExpenseDate <= endDate)
                    .OrderByDescending(e => e.ExpenseDate)
                    .ToListAsync();

                if (!expenses.Any())
                {
                    _logger.LogWarning("No expenses found for user {UserId} in date range", userId);
                    return await GenerateEmptyReport(userId, startDate, endDate);
                }

                using var package = new ExcelPackage();

                await CreateExpensesWorksheet(package, expenses, startDate, endDate);

                await CreateSummaryWorksheet(package, expenses, startDate, endDate);

                var result = package.GetAsByteArray();

                _logger.LogInformation("Successfully generated expense report for user {UserId}, file size: {Size} bytes",
                    userId, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating expense report for user {UserId}: {Message}", userId, ex.Message);
                throw new InvalidOperationException($"Failed to generate expense report: {ex.Message}", ex);
            }
        }

        private async Task CreateExpensesWorksheet(ExcelPackage package, List<Infrastructure.Database.Entities.Expense> expenses, DateTime startDate, DateTime endDate)
        {
            var worksheet = package.Workbook.Worksheets.Add("Expenses");

            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Category";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Amount";

            using (var range = worksheet.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thick);
            }

            for (int i = 0; i < expenses.Count; i++)
            {
                var expense = expenses[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = expense.ExpenseDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = expense.Category?.Name ?? "Unknown";
                worksheet.Cells[row, 3].Value = expense.Description;
                worksheet.Cells[row, 4].Value = expense.Amount;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
            }

            var totalRow = expenses.Count + 3;
            worksheet.Cells[totalRow, 3].Value = "Total:";
            worksheet.Cells[totalRow, 4].Value = expenses.Sum(e => e.Amount);
            worksheet.Cells[totalRow, 4].Style.Numberformat.Format = "$#,##0.00";

            using (var range = worksheet.Cells[totalRow, 3, totalRow, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            worksheet.Cells.AutoFitColumns();

            worksheet.Column(1).Width = 12;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 40;
            worksheet.Column(4).Width = 15;

            await Task.CompletedTask;
        }

        private async Task CreateSummaryWorksheet(ExcelPackage package, List<Infrastructure.Database.Entities.Expense> expenses, DateTime startDate, DateTime endDate)
        {
            var worksheet = package.Workbook.Worksheets.Add("Summary");

            worksheet.Cells[1, 1].Value = "Expense Report Summary";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            worksheet.Cells[2, 1].Value = $"Period: {startDate:MMMM d, yyyy} - {endDate:MMMM d, yyyy}";
            worksheet.Cells[3, 1].Value = $"Generated: {DateTime.Now:MMMM d, yyyy 'at' h:mm tt}";

            var totalAmount = expenses.Sum(e => e.Amount);
            var totalExpenses = expenses.Count;
            var averageExpense = totalExpenses > 0 ? totalAmount / totalExpenses : 0;

            worksheet.Cells[5, 1].Value = "Total Amount:";
            worksheet.Cells[5, 2].Value = totalAmount;
            worksheet.Cells[5, 2].Style.Numberformat.Format = "$#,##0.00";

            worksheet.Cells[6, 1].Value = "Total Expenses:";
            worksheet.Cells[6, 2].Value = totalExpenses;

            worksheet.Cells[7, 1].Value = "Average Expense:";
            worksheet.Cells[7, 2].Value = averageExpense;
            worksheet.Cells[7, 2].Style.Numberformat.Format = "$#,##0.00";

            var categoryBreakdown = expenses
                .GroupBy(e => e.Category?.Name ?? "Unknown")
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Percentage = totalAmount > 0 ? (g.Sum(e => e.Amount) / totalAmount) * 100 : 0
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            worksheet.Cells[10, 1].Value = "Category Breakdown";
            worksheet.Cells[10, 1].Style.Font.Size = 14;
            worksheet.Cells[10, 1].Style.Font.Bold = true;

            worksheet.Cells[12, 1].Value = "Category";
            worksheet.Cells[12, 2].Value = "Amount";
            worksheet.Cells[12, 3].Value = "Count";
            worksheet.Cells[12, 4].Value = "Percentage";

            using (var range = worksheet.Cells[12, 1, 12, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            for (int i = 0; i < categoryBreakdown.Count; i++)
            {
                var row = 13 + i;
                var category = categoryBreakdown[i];

                worksheet.Cells[row, 1].Value = category.Category;
                worksheet.Cells[row, 2].Value = category.Amount;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 3].Value = category.Count;
                worksheet.Cells[row, 4].Value = category.Percentage / 100;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "0.00%";
            }

            worksheet.Cells.AutoFitColumns();

            await Task.CompletedTask;
        }

        private async Task<byte[]> GenerateEmptyReport(int userId, DateTime startDate, DateTime endDate)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("No Data");

            worksheet.Cells[1, 1].Value = "Expense Report";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            worksheet.Cells[2, 1].Value = $"Period: {startDate:MMMM d, yyyy} - {endDate:MMMM d, yyyy}";
            worksheet.Cells[4, 1].Value = "No expenses found for this period.";

            worksheet.Cells.AutoFitColumns();

            return await Task.FromResult(package.GetAsByteArray());
        }
    }
}
