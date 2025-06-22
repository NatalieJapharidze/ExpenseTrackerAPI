using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;

namespace ExpenseTrackerApi.Features.Expenses
{
    public class CreateExpense
    {
        public record CreateCommand(int UserId, int CategoryId, decimal Amount, string Description, DateTime ExpenseDate);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                CreateCommand command,
                IRepository<Expense> repository,
                ILogger<CreateExpense> logger)
            {
                try
                {
                    if (command.UserId <= 0)
                        return Results.BadRequest(new { Message = "Invalid user ID" });

                    if (command.CategoryId <= 0)
                        return Results.BadRequest(new { Message = "Invalid category ID" });

                    if (command.Amount <= 0)
                        return Results.BadRequest(new { Message = "Amount must be greater than 0" });

                    if (string.IsNullOrWhiteSpace(command.Description))
                        return Results.BadRequest(new { Message = "Description cannot be empty" });

                    if (command.ExpenseDate > DateTime.UtcNow.AddDays(1))
                        return Results.BadRequest(new { Message = "Expense date cannot be in the future" });

                    var expense = new Expense
                    {
                        UserId = command.UserId,
                        CategoryId = command.CategoryId,
                        Amount = command.Amount,
                        Description = command.Description.Trim(),
                        ExpenseDate = DateTime.SpecifyKind(command.ExpenseDate, DateTimeKind.Utc),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await repository.AddAsync(expense);

                    logger.LogInformation("Created expense {ExpenseId} for user {UserId}", expense.Id, command.UserId);

                    return Results.Created($"/api/expenses/{expense.Id}", expense);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating expense for user {UserId}: {Message}", command.UserId, ex.Message);
                    return Results.Problem("An error occurred while creating the expense");
                }
            }
        }
    }
}
