using System.Text.RegularExpressions;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackerApi.Features.Users
{
    public class UpdateUser
    {
        public record UpdateUserCommand(string FullName, string CurrencyCode);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int id,
                UpdateUserCommand command,
                [FromQuery] int requestingUserId,
                IRepository<User> repository,
                ILogger<UpdateUser> logger)
            {
                try
                {
                    var validationResult = ValidateInput(id, requestingUserId, command);
                    if (!validationResult.IsValid)
                    {
                        return Results.BadRequest(new { Message = validationResult.ErrorMessage });
                    }

                    var user = await repository.GetByIdAsync(id);
                    if (user == null)
                    {
                        logger.LogInformation("User not found for update: {UserId}", id);
                        return Results.NotFound(new { Message = "User not found" });
                    }

                    if (user.Id != requestingUserId)
                    {
                        logger.LogWarning("User {RequestingUserId} attempted to update user {TargetUserId}",
                            requestingUserId, id);
                        return Results.Forbid();
                    }

                    var originalFullName = user.FullName;
                    var originalCurrencyCode = user.CurrencyCode;

                    user.FullName = command.FullName.Trim();
                    user.CurrencyCode = command.CurrencyCode.ToUpper().Trim();

                    await repository.UpdateAsync(user);

                    logger.LogInformation("Updated user {UserId}: FullName '{OldName}' → '{NewName}', " +
                                        "CurrencyCode '{OldCurrency}' → '{NewCurrency}'",
                        id, originalFullName, user.FullName, originalCurrencyCode, user.CurrencyCode);

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
                    logger.LogError(ex, "Error updating user {UserId}: {Message}", id, ex.Message);
                    return Results.Problem("An error occurred while updating the user");
                }
            }

            private static (bool IsValid, string ErrorMessage) ValidateInput(int id, int requestingUserId, UpdateUserCommand command)
            {
                if (id <= 0)
                    return (false, "Invalid user ID");

                if (requestingUserId <= 0)
                    return (false, "Invalid requesting user ID");

                if (string.IsNullOrWhiteSpace(command.FullName))
                    return (false, "Full name is required");

                if (command.FullName.Length > 255)
                    return (false, "Full name cannot exceed 255 characters");

                if (string.IsNullOrWhiteSpace(command.CurrencyCode))
                    return (false, "Currency code is required");

                if (command.CurrencyCode.Length != 3)
                    return (false, "Currency code must be exactly 3 characters (e.g., USD, EUR, GEL)");

                if (!Regex.IsMatch(command.CurrencyCode, @"^[A-Za-z]{3}$"))
                    return (false, "Currency code must contain only letters");

                return (true, string.Empty);
            }
        }
    }
}
