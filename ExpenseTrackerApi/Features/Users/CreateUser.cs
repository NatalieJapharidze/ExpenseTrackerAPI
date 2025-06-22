using System.Text.RegularExpressions;
using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Features.Users
{
    public class CreateUser
    {
        public record CreateUserCommand(string Email, string FullName, string CurrencyCode);

        public class Endpoint
        {
            public static async Task<IResult> Handle(
                CreateUserCommand command,
                IRepository<User> repository,
                AppDbContext context,
                ILogger<CreateUser> logger)
            {
                try
                {
                    var validationResult = ValidateInput(command);
                    if (!validationResult.IsValid)
                    {
                        return Results.BadRequest(new { Message = validationResult.ErrorMessage });
                    }

                    var normalizedEmail = command.Email.ToLower().Trim();

                    var existingUser = await context.Users
                        .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                    if (existingUser != null)
                    {
                        logger.LogWarning("Attempt to create user with existing email: {Email}", normalizedEmail);
                        return Results.Conflict(new { Message = "User with this email already exists" });
                    }

                    var user = new User
                    {
                        Email = normalizedEmail,
                        FullName = command.FullName.Trim(),
                        CurrencyCode = command.CurrencyCode.ToUpper().Trim(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await repository.AddAsync(user);

                    logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);

                    var response = new
                    {
                        user.Id,
                        user.Email,
                        user.FullName,
                        user.CurrencyCode,
                        user.CreatedAt
                    };

                    return Results.Created($"/api/users/{user.Id}", response);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true ||
                                                   ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    logger.LogWarning("Database constraint violation creating user with email: {Email}", command.Email);
                    return Results.Conflict(new { Message = "User with this email already exists" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating user with email: {Email}", command.Email);
                    return Results.Problem("An error occurred while creating the user");
                }
            }

            private static (bool IsValid, string ErrorMessage) ValidateInput(CreateUserCommand command)
            {
                if (string.IsNullOrWhiteSpace(command.Email))
                    return (false, "Email is required");

                if (command.Email.Length > 255)
                    return (false, "Email cannot exceed 255 characters");

                if (!IsValidEmail(command.Email))
                    return (false, "Invalid email format");

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

            private static bool IsValidEmail(string email)
            {
                try
                {
                    var emailAddress = new System.Net.Mail.MailAddress(email);
                    return emailAddress.Address == email;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
