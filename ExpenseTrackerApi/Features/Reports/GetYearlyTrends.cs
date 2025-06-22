using ExpenseTrackerApi.Common.Specifications;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Reports
{
    public class GetYearlyTrends
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int year,
                int userId,
                IRepository<Expense> repository,
                ICacheService cache)
            {
                var cacheKey = $"yearly_trends_{userId}_{year}";

                var trends = await cache.GetOrCreateAsync(
                    cacheKey,
                    async () => await GenerateTrends(userId, year, repository),
                    TimeSpan.FromHours(6)
                );

                return Results.Ok(trends);
            }

            private static async Task<object> GenerateTrends(int userId, int year, IRepository<Expense> repository)
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);

                var spec = new ExpensesByDateRangeSpec(userId, startDate, endDate);
                var expenses = await repository.ListAsync(spec);

                var monthlyTrends = expenses
                    .GroupBy(e => e.ExpenseDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        MonthName = new DateTime(year, g.Key, 1).ToString("MMMM"),
                        TotalAmount = g.Sum(e => e.Amount),
                        ExpenseCount = g.Count(),
                        AverageAmount = g.Average(e => e.Amount)
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                return new
                {
                    Year = year,
                    TotalAmount = expenses.Sum(e => e.Amount),
                    TotalExpenses = expenses.Count,
                    MonthlyTrends = monthlyTrends,
                    TopCategories = expenses
                        .GroupBy(e => e.Category.Name)
                        .Select(g => new
                        {
                            Category = g.Key,
                            Amount = g.Sum(e => e.Amount),
                            Percentage = Math.Round((g.Sum(e => e.Amount) / expenses.Sum(e => e.Amount)) * 100, 2)
                        })
                        .OrderByDescending(x => x.Amount)
                        .Take(5)
                        .ToList()
                };
            }
        }
    }
}
