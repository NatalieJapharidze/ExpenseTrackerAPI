using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Features.Categories
{
    public class DeleteCategory
    {
        public record DeleteCategoryCommand(int UserId, bool Force = false);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int id,
                IRepository<Category> repository,
                ICacheService cache,
                AppDbContext context,
                ILogger<DeleteCategory> logger,
                [FromQuery] int userId,
                [FromQuery] bool force = false)
            {
                var command = new DeleteCategoryCommand(userId, force);

                try
                {
                    var category = await context.Categories
                        .Include(c => c.Expenses)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (category == null)
                    {
                        return Results.NotFound(new { Message = "Category not found" });
                    }

                    if (category.UserId != command.UserId)
                    {
                        return Results.Forbid();
                    }

                    var expenseCount = category.Expenses.Count;

                    if (expenseCount > 0 && !command.Force)
                    {
                        return Results.BadRequest(new
                        {
                            Message = $"Category '{category.Name}' has {expenseCount} associated expenses.",
                            CategoryId = category.Id,
                            ExpenseCount = expenseCount,
                            Suggestion = "Set 'force' to true to delete category and all its expenses, or deactivate the category instead."
                        });
                    }

                    if (command.Force && expenseCount > 0)
                    {
                        var expenses = await context.Expenses
                            .Where(e => e.CategoryId == id)
                            .ToListAsync();

                        context.Expenses.RemoveRange(expenses);

                        logger.LogWarning("Force deleting category '{CategoryName}' and {ExpenseCount} associated expenses for user {UserId}",
                            category.Name, expenseCount, command.UserId);
                    }

                    await repository.DeleteAsync(category);

                    await cache.RemoveAsync($"categories_{command.UserId}");

                    logger.LogInformation("Deleted category '{CategoryName}' (ID: {CategoryId}) for user {UserId}",
                        category.Name, category.Id, command.UserId);

                    return Results.Ok(new
                    {
                        Message = command.Force && expenseCount > 0
                            ? $"Category '{category.Name}' and {expenseCount} associated expenses have been deleted"
                            : $"Category '{category.Name}' has been deleted",
                        CategoryId = category.Id,
                        DeletedExpenses = command.Force ? expenseCount : 0,
                        Action = command.Force ? "force_delete" : "delete"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deleting category {CategoryId} for user {UserId}", id, command.UserId);
                    return Results.Problem("An error occurred while deleting the category" );
                }
            }
        }
    }
}
