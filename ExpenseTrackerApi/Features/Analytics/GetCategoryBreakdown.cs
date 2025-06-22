using ExpenseTrackerApi.Common.Specifications;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Analytics
{
    public class GetCategoryBreakdown
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
            int userId,
            IRepository<Expense> repository,
            ICacheService cache,
            DateTime? startDate = null,
            DateTime? endDate = null)
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var cacheKey = $"category_breakdown_{userId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

                var breakdown = await cache.GetOrCreateAsync(
                    cacheKey,
                    async () => await GenerateBreakdown(userId, startDate.Value, endDate.Value, repository),
                    TimeSpan.FromMinutes(30)
                );

                return Results.Ok(breakdown);
            }

            private static async Task<object> GenerateBreakdown(
                    int userId,
                    DateTime startDate,
                    DateTime endDate,
                    IRepository<Expense> repository)
            {
                var spec = new ExpensesByDateRangeSpec(userId, startDate, endDate);
                var expenses = await repository.ListAsync(spec);

                var totalAmount = expenses.Sum(e => e.Amount);

                var breakdown = expenses
                    .GroupBy(e => new { e.Category.Id, e.Category.Name, e.Category.ColorHex })
                    .Select(g => new
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.Name,
                        ColorHex = g.Key.ColorHex,
                        Amount = g.Sum(e => e.Amount),
                        ExpenseCount = g.Count(),
                        Percentage = totalAmount > 0 ? Math.Round((g.Sum(e => e.Amount) / totalAmount) * 100, 2) : 0,
                        AverageExpense = g.Average(e => e.Amount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                return new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    TotalAmount = totalAmount,
                    TotalExpenses = expenses.Count,
                    Categories = breakdown
                };
            }
        }
    }
}
