using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;

namespace ExpenseTrackerApi.Features.Users
{
    public class GetUser
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int id,
                IRepository<User> repository,
                ILogger<GetUser> logger)
            {
                try
                {
                    if (id <= 0)
                    {
                        logger.LogWarning("Invalid user ID requested: {UserId}", id);
                        return Results.BadRequest(new { Message = "Invalid user ID" });
                    }

                    var user = await repository.GetByIdAsync(id);
                    if (user == null)
                    {
                        logger.LogInformation("User not found: {UserId}", id);
                        return Results.NotFound(new { Message = "User not found" });
                    }

                    logger.LogInformation("Retrieved user {UserId}", id);

                    var response = new
                    {
                        user.Id,
                        user.Email,
                        user.FullName,
                        user.CurrencyCode,
                        user.CreatedAt
                    };

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving user {UserId}: {Message}", id, ex.Message);
                    return Results.Problem("An error occurred while retrieving the user");
                }
            }
        }
    }

}
