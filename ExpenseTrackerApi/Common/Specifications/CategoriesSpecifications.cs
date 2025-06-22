using ExpenseTrackerApi.Infrastructure.Database.Entities;

namespace ExpenseTrackerApi.Common.Specifications
{
    public class CategoriesByUserSpec : BaseSpecification<Category>
    {
        public CategoriesByUserSpec(int userId)
            : base(c => c.UserId == userId && c.IsActive)
        {
            AddOrderBy(c => c.Name);
        }
    }
}
