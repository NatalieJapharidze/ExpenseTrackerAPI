using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTrackerApi.Features.Analytics;
using ExpenseTrackerApi.Features.Categories;
using ExpenseTrackerApi.Features.Expenses;
using ExpenseTrackerApi.Features.Reports;
using ExpenseTrackerApi.Features.Users;
using ExpenseTrackerApi.Infrastructure.BackgroundJobs;
using ExpenseTrackerApi.Infrastructure.Database;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using StackExchange.Redis;

namespace ExpenseTrackerApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAntiforgery();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(provider => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddScoped<IExcelService, ExcelService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            builder.Services.AddHostedService<ReportGenerationService>();
            builder.Services.AddHostedService<BudgetAlertService>();
            builder.Services.AddHostedService<MonthlyEmailService>();

            builder.Services.AddMemoryCache();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "💰 Expense Tracker API",
                    Version = "v1.0",
                    Description = @"
**A modern expense tracking system with advanced features:**

🔹 **Expense Management** - CRUD operations with bulk import  
🔹 **Category & Budget Management** - Smart budget alerts  
🔹 **Analytics & Reports** - Excel exports & email reports  
🔹 **Real-time Caching** - Redis + In-Memory for performance  

---
### 🚀 **Quick Start:**
1. Create a user with `POST /api/users`
2. Add categories with `POST /api/categories` 
3. Track expenses with `POST /api/expenses`
4. View analytics with `GET /api/analytics/category-breakdown`

---
"
                });

                c.TagActionsBy(api => new[] { GetTagForController(api) });
                c.DocInclusionPredicate((name, api) => true);

                c.OrderActionsBy((apiDesc) => $"{GetTagForController(apiDesc)}_{apiDesc.HttpMethod}_{apiDesc.RelativePath}");
            });

            builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            var app = builder.Build();

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();

                    if (exceptionFeature?.Error != null)
                    {
                        logger.LogError(exceptionFeature.Error, "Unhandled exception occurred");

                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            Message = "An error occurred while processing your request",
                            Error = app.Environment.IsDevelopment() ? exceptionFeature.Error.Message : "Internal Server Error"
                        }));
                    }
                });
            });

            app.UseStaticFiles();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Tracker API v1");
                    c.RoutePrefix = "api/docs";
                    c.DocumentTitle = "💰 Expense Tracker API";

                    c.DefaultModelsExpandDepth(-1);
                    c.DefaultModelExpandDepth(2);
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                    c.EnableDeepLinking();
                    c.EnableFilter();
                    c.EnableValidator();

                    c.InjectStylesheet("/swagger-ui/custom.css");
                    c.InjectJavascript("/swagger-ui/custom.js");

                    c.HeadContent = @"
                        <style>
                            .swagger-ui .topbar { display: none !important; }
                            .swagger-ui .info .title { 
                                color: #2c3e50 !important; 
                                font-size: 2.5rem !important;
                                text-align: center !important;
                                margin-bottom: 1rem !important;
                            }
                        </style>";
                });
            }

            app.MapPost("/api/users", CreateUser.Endpoint.Handle);
            app.MapGet("/api/users/{id}",
                (int id, IRepository<User> repository, ILogger<GetUser> logger)
                    => GetUser.Endpoint.Handle(id, repository, logger));
            app.MapPut("/api/users/{id}",
                (int id, UpdateUser.UpdateUserCommand command, [FromQuery] int requestingUserId,
                  IRepository<User> repository, ILogger<UpdateUser> logger)
                    => UpdateUser.Endpoint.Handle(id, command, requestingUserId, repository, logger));

            app.MapPost("/api/expenses", CreateExpense.Endpoint.Handle);
            app.MapGet("/api/expenses", GetExpenses.Endpoint.Handle);
            app.MapPut("/api/expenses/{id}", UpdateExpense.Endpoint.Handle);
            app.MapDelete("/api/expenses/{id}",
                (int id, [FromQuery] int userId, IRepository<Expense> repository, ILogger<DeleteExpense> logger) =>
                DeleteExpense.Endpoint.Handle(id, userId, repository, logger));
            app.MapPost("/api/expenses/import", ImportExpenses.Endpoint.Handle).DisableAntiforgery();

            app.MapGet("/api/categories", GetCategories.Endpoint.Handle);
            app.MapPost("/api/categories", CreateCategory.Endpoint.Handle);
            app.MapPut("/api/categories/{id}/budget",
                (int id, UpdateBudget.UpdateBudgetCommand command,
                IRepository<Category> repository, ICacheService cache, ILogger<UpdateBudget> logger)
                    => UpdateBudget.Endpoint.Handle(id, command, repository, cache, logger));
            app.MapDelete("/api/categories/{id}", DeleteCategory.Endpoint.Handle);

            app.MapGet("/api/reports/monthly/{year}/{month}", GetMonthlyReport.Endpoint.Handle);
            app.MapGet("/api/reports/yearly/{year}", GetYearlyTrends.Endpoint.Handle);
            app.MapPost("/api/reports/excel", GenerateExcelReport.Endpoint.Handle);
            app.MapPost("/api/reports/email", SendEmailReport.Endpoint.Handle);
            app.MapGet("/api/analytics/category-breakdown", GetCategoryBreakdown.Endpoint.Handle);

            var reportsPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "reports");
            Directory.CreateDirectory(reportsPath);

            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
                await dbInitializer.InitializeAsync();
            }

            app.Run();
        }

        static string GetTagForController(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription api)
        {
            var path = api.RelativePath?.ToLower() ?? "";

            if (path.Contains("users")) return "👤 User Management";
            if (path.Contains("expenses")) return "💸 Expense Management";
            if (path.Contains("categories")) return "📁 Category Management";
            if (path.Contains("reports")) return "📊 Reports & Analytics";
            if (path.Contains("analytics")) return "📈 Analytics Dashboard";

            return "🔧 API Operations";
        }
    }
}