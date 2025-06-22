using CsvHelper;
using CsvHelper.Configuration;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Globalization;

namespace ExpenseTrackerApi.Features.Expenses
{
    public class ImportExpenses
    {
        public record ExpenseRecord(DateTime Date, string Category, string Description, decimal Amount, int RowNumber);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                IFormFile file,
                int userId,
                IRepository<Expense> expenseRepository,
                IRepository<Category> categoryRepository)
            {
                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ImportExpenses>();

                try
                {
                    if (file == null || file.Length == 0)
                        return Results.BadRequest(new { Message = "No file uploaded" });

                    var allowedExtensions = new[] { ".csv", ".txt", ".xlsx", ".xls" };
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return Results.BadRequest(new
                        {
                            Message = "Only CSV, TXT, and Excel files are allowed",
                            AllowedTypes = string.Join(", ", allowedExtensions)
                        });
                    }

                    var categories = await categoryRepository.ListAllAsync();
                    var categoryDict = new Dictionary<string, int>();
                    foreach (var category in categories)
                    {
                        var categoryKey = category.Name.ToLower().Trim();
                        if (!categoryDict.ContainsKey(categoryKey))
                        {
                            categoryDict[categoryKey] = category.Id;
                        }
                    }

                    List<ExpenseRecord> records;
                    if (fileExtension == ".xlsx" || fileExtension == ".xls")
                    {
                        records = await ParseExcelFile(file, logger);
                    }
                    else
                    {
                        records = await ParseCsvFile(file, logger);
                    }

                    if (!records.Any())
                    {
                        return Results.BadRequest(new
                        {
                            Message = "No valid data found in file",
                            ExpectedFormat = "Columns: Date, Category, Description, Amount"
                        });
                    }

                    var expenses = new List<Expense>();
                    var errors = new List<string>();
                    var skippedRows = 0;

                    foreach (var record in records)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(record.Category) ||
                                string.IsNullOrWhiteSpace(record.Description))
                            {
                                errors.Add($"Row {record.RowNumber}: Category and Description cannot be empty");
                                skippedRows++;
                                continue;
                            }

                            if (record.Amount <= 0)
                            {
                                errors.Add($"Row {record.RowNumber}: Amount must be greater than 0");
                                skippedRows++;
                                continue;
                            }

                            var categoryKey = record.Category.ToLower().Trim();
                            int createdCategoryId = 0;
                            if (!categoryDict.TryGetValue(categoryKey, out var categoryId))
                            {
                                var createdCategory = await categoryRepository.AddAsync(new Category
                                {
                                    Name = record.Category,
                                    MonthlyBudget = 0,
                                    UserId = userId,
                                    Icon = "",
                                    ColorHex = "#000000",
                                    IsActive = true
                                });
                                createdCategoryId = createdCategory.Id;
                                categoryDict[categoryKey] = createdCategoryId;
                            }

                            var expense = new Expense
                            {
                                UserId = userId,
                                CategoryId = categoryId == 0 ? createdCategoryId : categoryId,
                                Amount = record.Amount,
                                Description = record.Description.Trim(),
                                ExpenseDate = DateTime.SpecifyKind(record.Date, DateTimeKind.Utc),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            expenses.Add(expense);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing row {RowNumber}", record.RowNumber);
                            errors.Add($"Row {record.RowNumber}: {ex.Message}");
                            skippedRows++;
                        }
                    }

                    var savedCount = 0;
                    foreach (var expense in expenses)
                    {
                        try
                        {
                            await expenseRepository.AddAsync(expense);
                            savedCount++;
                        }
                        catch (Exception saveEx)
                        {
                            logger.LogError(saveEx, "Error saving expense");
                            errors.Add($"Failed to save expense: {expense.Description}");
                        }
                    }

                    var result = new
                    {
                        Message = savedCount > 0
                            ? $"Import completed. {savedCount} expenses imported successfully."
                            : "Import completed but no valid expenses were found.",
                        FileName = file.FileName,
                        FileType = fileExtension.ToUpper().Replace(".", ""),
                        ImportedCount = savedCount,
                        SkippedRows = skippedRows,
                        TotalRows = records.Count,
                        Errors = errors.Take(20).ToList(),
                        HasMoreErrors = errors.Count > 20,
                        AvailableCategories = categoryDict.Keys.ToList()
                    };

                    return savedCount > 0
                        ? Results.Ok(result)
                        : Results.BadRequest(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error during import");
                    return Results.Problem($"An unexpected error occurred during import{ex.Message}");
                }
            }

            private static async Task<List<ExpenseRecord>> ParseExcelFile(IFormFile file, ILogger logger)
            {
                var records = new List<ExpenseRecord>();

                try
                {
                    using var stream = file.OpenReadStream();
                    using var package = new ExcelPackage(stream);

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        logger.LogWarning("Excel file has no worksheets");
                        return records;
                    }

                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        logger.LogWarning("Excel file has insufficient rows");
                        return records;
                    }

                    logger.LogInformation("Processing Excel file with {RowCount} rows", rowCount);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var dateCell = worksheet.Cells[row, 1].Value;
                            var categoryCell = worksheet.Cells[row, 2].Value;
                            var descriptionCell = worksheet.Cells[row, 3].Value;
                            var amountCell = worksheet.Cells[row, 4].Value;

                            if (dateCell == null && categoryCell == null && descriptionCell == null && amountCell == null)
                                continue;

                            DateTime expenseDate;
                            if (dateCell is DateTime dt)
                            {
                                expenseDate = dt;
                            }
                            else if (DateTime.TryParse(dateCell?.ToString(), out var parsedDate))
                            {
                                expenseDate = parsedDate;
                            }
                            else
                            {
                                continue;
                            }

                            decimal amount = 0;
                            if (amountCell is double doubleValue)
                            {
                                amount = (decimal)doubleValue;
                            }
                            else if (amountCell is decimal decimalValue)
                            {
                                amount = decimalValue;
                            }
                            else if (decimal.TryParse(amountCell?.ToString()?.Replace("$", "").Replace(",", ""), out var parsedAmount))
                            {
                                amount = parsedAmount;
                            }

                            if (amount <= 0)
                                continue;

                            var category = categoryCell?.ToString()?.Trim() ?? "";
                            var description = descriptionCell?.ToString()?.Trim() ?? "";

                            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(description))
                                continue;

                            records.Add(new ExpenseRecord(expenseDate, category, description, amount, row));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error parsing Excel row {Row}", row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading Excel file");
                }

                return records;
            }

            private static async Task<List<ExpenseRecord>> ParseCsvFile(IFormFile file, ILogger logger)
            {
                var records = new List<ExpenseRecord>();

                try
                {
                    using var reader = new StringReader(await new StreamReader(file.OpenReadStream()).ReadToEndAsync());

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        BadDataFound = null,
                        MissingFieldFound = null,
                        HeaderValidated = null,
                        TrimOptions = TrimOptions.Trim,
                        IgnoreBlankLines = true,
                        HasHeaderRecord = true,
                        DetectDelimiter = true
                    };

                    using var csv = new CsvReader(reader, config);

                    csv.Read();
                    csv.ReadHeader();

                    var rowNumber = 1;
                    while (csv.Read())
                    {
                        rowNumber++;
                        try
                        {
                            if (csv.Parser.Count < 4)
                                continue;

                            var dateStr = csv.GetField(0)?.Trim();
                            var categoryStr = csv.GetField(1)?.Trim();
                            var descriptionStr = csv.GetField(2)?.Trim();
                            var amountStr = csv.GetField(3)?.Trim();

                            if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(categoryStr) ||
                                string.IsNullOrEmpty(descriptionStr) || string.IsNullOrEmpty(amountStr))
                                continue;

                            if (!DateTime.TryParse(dateStr, out var expenseDate))
                                continue;

                            if (!decimal.TryParse(amountStr.Replace("$", "").Replace(",", ""), out var amount) || amount <= 0)
                                continue;

                            records.Add(new ExpenseRecord(expenseDate, categoryStr, descriptionStr, amount, rowNumber));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error parsing CSV row {RowNumber}", rowNumber);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading CSV file");
                }

                return records;
            }
        }
    }
}