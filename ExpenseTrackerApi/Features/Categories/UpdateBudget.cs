using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Categories
{
    public class UpdateBudget
    {
        public record UpdateBudgetCommand(int UserId, decimal MonthlyBudget);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int id,
                UpdateBudgetCommand command,
                IRepository<Category> repository,
                ICacheService cache,
                ILogger<UpdateBudget> logger)
            {
                try
                {
                    if (command.UserId <= 0)
                        return Results.BadRequest(new { Message = "Invalid user ID" });

                    if (command.MonthlyBudget < 0)
                        return Results.BadRequest(new { Message = "Monthly budget cannot be negative" });

                    var category = await repository.GetByIdAsync(id);
                    if (category == null)
                        return Results.NotFound(new { Message = "Category not found" });

                    if (category.UserId != command.UserId)
                        return Results.Forbid();

                    category.MonthlyBudget = command.MonthlyBudget;
                    await repository.UpdateAsync(category);

                    await cache.RemoveAsync($"categories_{command.UserId}");

                    logger.LogInformation("Updated budget for category {CategoryId} to {Budget} for user {UserId}",
                        id, command.MonthlyBudget, command.UserId);

                    return Results.Ok(category);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating budget for category {CategoryId} for user {UserId}",
                        id, command.UserId);
                    return Results.Problem("An error occurred while updating the budget");
                }
            }
        }
    }
}
