using ExpenseTrackerApi.Common.Specifications;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using ExpenseTrackerApi.Infrastructure.Repositories;
using ExpenseTrackerApi.Infrastructure.Services;

namespace ExpenseTrackerApi.Features.Categories
{
    public class GetCategories
    {
        public class Endpoint
        {
            public static async Task<IResult> Handle(
                int userId,
                IRepository<Category> repository,
                ICacheService cache)
            {
                var cacheKey = $"categories_{userId}";

                var categories = await cache.GetOrCreateAsync(
                    cacheKey,
                    async () => await repository.ListAsync(new CategoriesByUserSpec(userId)),
                    TimeSpan.FromMinutes(15)
                );

                return Results.Ok(categories);
            }
        }
    }
}
