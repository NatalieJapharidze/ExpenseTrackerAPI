using ExpenseTrackerApi.Common.Specifications;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackerApi.Features.Expenses
{
    public class GetExpenses
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
                IRepository<Expense> repository,
                [FromQuery] int userId,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] DateTime? startDate = null,
                [FromQuery] DateTime? endDate = null,
                [FromQuery] int? categoryId = null)
            {
                if (userId <= 0)
                    return Results.BadRequest(new { Message = "Invalid user ID" });

                if (page < 1)
                    page = 1;

                if (pageSize < 1 || pageSize > 100)
                    pageSize = 10;

                var skip = (page - 1) * pageSize;

                ISpecification<Expense> spec;
                ISpecification<Expense> countSpec;

                if (startDate.HasValue && endDate.HasValue)
                {
                    spec = new ExpensesByDateRangeWithPaginationSpec(userId, startDate.Value, endDate.Value, skip, pageSize, categoryId);
                    countSpec = new ExpensesByDateRangeSpec(userId, startDate.Value, endDate.Value);
                }
                else
                {
                    spec = new ExpensesWithPaginationSpec(userId, skip, pageSize, categoryId);
                    countSpec = new ExpensesByUserSpec(userId);
                }

                var expenses = await repository.ListAsync(spec);
                var total = await repository.CountAsync(countSpec);

                return Results.Ok(new
                {
                    Data = expenses,
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize),
                    HasNextPage = page * pageSize < total,
                    HasPreviousPage = page > 1
                });
            }
        }
    }
}
