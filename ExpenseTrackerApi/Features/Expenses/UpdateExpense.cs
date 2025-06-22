using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;

namespace ExpenseTrackerApi.Features.Expenses
{
    public class UpdateExpense
    {
        public record UpdateCommand(int UserId, int CategoryId, decimal Amount, string Description, DateTime ExpenseDate);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int id,
                UpdateCommand command,
                IRepository<Expense> repository,
                ILogger<UpdateExpense> logger)
            {
                try
                {
                    if (command.UserId <= 0)
                        return Results.BadRequest(new { Message = "Invalid user ID" });

                    if (command.Amount <= 0)
                        return Results.BadRequest(new { Message = "Amount must be greater than 0" });

                    if (string.IsNullOrWhiteSpace(command.Description))
                        return Results.BadRequest(new { Message = "Description cannot be empty" });

                    if (command.ExpenseDate > DateTime.UtcNow.AddDays(1))
                        return Results.BadRequest(new { Message = "Expense date cannot be in the future" });

                    var expense = await repository.GetByIdAsync(id);
                    if (expense == null)
                        return Results.NotFound(new { Message = "Expense not found" });

                    if (expense.UserId != command.UserId)
                        return Results.Forbid();

                    expense.CategoryId = command.CategoryId;
                    expense.Amount = command.Amount;
                    expense.Description = command.Description.Trim();
                    expense.ExpenseDate = DateTime.SpecifyKind(command.ExpenseDate, DateTimeKind.Utc);
                    expense.UpdatedAt = DateTime.UtcNow;

                    await repository.UpdateAsync(expense);

                    logger.LogInformation("Updated expense {ExpenseId} for user {UserId}", id, command.UserId);

                    return Results.Ok(expense);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating expense {ExpenseId} for user {UserId}", id, command.UserId);
                    return Results.Problem("An error occurred while updating the expense");
                }
            }
        }
    }
}
