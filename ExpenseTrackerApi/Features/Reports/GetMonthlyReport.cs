using ExpenseTrackerApi.Common.Specifications;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Reports
{
    public class GetMonthlyReport
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int year,
                int month,
                int userId,
                IRepository<Expense> repository,
                ICacheService cache)
            {
                var cacheKey = $"monthly_report_{userId}_{year}_{month}";

                var report = await cache.GetOrCreateAsync(
                    cacheKey,
                    async () => await GenerateReport(userId, year, month, repository),
                    TimeSpan.FromHours(1)
                );

                return Results.Ok(report);
            }

            private static async Task<object> GenerateReport(int userId, int year, int month, IRepository<Expense> repository)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var spec = new ExpensesByDateRangeSpec(userId, startDate, endDate);
                var expenses = await repository.ListAsync(spec);

                return new
                {
                    Month = $"{year}-{month:D2}",
                    TotalAmount = expenses.Sum(e => e.Amount),
                    ExpenseCount = expenses.Count,
                    CategoryBreakdown = expenses
                        .GroupBy(e => e.Category.Name)
                        .Select(g => new
                        {
                            Category = g.Key,
                            Amount = g.Sum(e => e.Amount),
                            Count = g.Count()
                        })
                        .ToList()
                };
            }
        }
    }
}
