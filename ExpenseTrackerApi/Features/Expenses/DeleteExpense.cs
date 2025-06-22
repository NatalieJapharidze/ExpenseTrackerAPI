using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;

namespace ExpenseTrackerApi.Features.Expenses
{
    public class DeleteExpense
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int id,
                int userId,
                IRepository<Expense> repository,
                ILogger<DeleteExpense> logger)
            {
                try
                {
                    if (userId <= 0)
                        return Results.BadRequest(new { Message = "Invalid user ID" });

                    var expense = await repository.GetByIdAsync(id);
                    if (expense == null)
                        return Results.NotFound(new { Message = "Expense not found" });

                    if (expense.UserId != userId)
                        return Results.Forbid();

                    await repository.DeleteAsync(expense);

                    logger.LogInformation("Deleted expense {ExpenseId} for user {UserId}", id, userId);

                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deleting expense {ExpenseId} for user {UserId}", id, userId);
                    return Results.Problem("An error occurred while deleting the expense");
                }
            }
        }
    }
}
