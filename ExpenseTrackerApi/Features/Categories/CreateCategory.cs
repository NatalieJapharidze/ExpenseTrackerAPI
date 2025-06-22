using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Features.Categories
{
    public class CreateCategory
    {
        public record CreateCategoryCommand(int UserId, string Name, string Icon, string ColorHex, decimal MonthlyBudget);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                CreateCategoryCommand command,
                IRepository<Category> repository,
                ICacheService cache,
                AppDbContext context,
                ILogger<CreateCategory> logger)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(command.Name))
                    {
                        return Results.BadRequest(new { Message = "Category name cannot be empty" });
                    }

                    if (command.MonthlyBudget < 0)
                    {
                        return Results.BadRequest(new { Message = "Monthly budget cannot be negative" });
                    }

                    var userExists = await context.Users.AnyAsync(u => u.Id == command.UserId);
                    if (!userExists)
                    {
                        return Results.NotFound(new { Message = "User not found" });
                    }

                    var categoryName = command.Name.Trim();
                    var existingCategory = await context.Categories
                        .Where(c => c.UserId == command.UserId &&
                                   c.IsActive &&
                                   c.Name.ToLower() == categoryName.ToLower())
                        .FirstOrDefaultAsync();

                    if (existingCategory != null)
                    {
                        return Results.Conflict(new
                        {
                            Message = $"Category '{categoryName}' already exists for this user",
                            ExistingCategoryId = existingCategory.Id
                        });
                    }

                    var category = new Category
                    {
                        UserId = command.UserId,
                        Name = categoryName,
                        Icon = command.Icon?.Trim() ?? "",
                        ColorHex = command.ColorHex?.Trim() ?? "#000000",
                        MonthlyBudget = command.MonthlyBudget,
                        IsActive = true
                    };

                    await repository.AddAsync(category);

                    await cache.RemoveAsync($"categories_{command.UserId}");

                    logger.LogInformation("Created category '{CategoryName}' for user {UserId}",
                        category.Name, command.UserId);

                    return Results.Created($"/api/categories/{category.Id}", category);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    logger.LogWarning("Attempt to create duplicate category '{CategoryName}' for user {UserId}",
                        command.Name, command.UserId);
                    return Results.Conflict(new { Message = $"Category '{command.Name}' already exists for this user" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating category '{CategoryName}' for user {UserId}",
                        command.Name, command.UserId);
                    return Results.Problem( "An error occurred while creating the category" );
                }
            }
        }
    }
}
