using ExpenseTrackerApi.Infrastructure.Database.Entities;

namespace ExpenseTrackerApi.Common.Specifications
{
    public class ExpensesByDateRangeSpec : BaseSpecification<Expense>
    {
        public ExpensesByDateRangeSpec(int userId, DateTime startDate, DateTime endDate)
            : base(e => e.UserId == userId && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
        {
            AddInclude(e => e.Category);
            AddOrderByDescending(e => e.ExpenseDate);
        }
    }

    public class ExpensesByDateRangeWithPaginationSpec : BaseSpecification<Expense>
    {
        public ExpensesByDateRangeWithPaginationSpec(int userId, DateTime startDate, DateTime endDate, int skip, int take, int? categoryId = null)
            : base(e => e.UserId == userId &&
                       e.ExpenseDate >= startDate &&
                       e.ExpenseDate <= endDate &&
                       (categoryId == null || e.CategoryId == categoryId))
        {
            AddInclude(e => e.Category);
            AddOrderByDescending(e => e.ExpenseDate);
            ApplyPaging(skip, take);
        }
    }
    public class ExpensesWithPaginationSpec : BaseSpecification<Expense>
    {
        public ExpensesWithPaginationSpec(int userId, int skip, int take, int? categoryId = null)
            : base(e => e.UserId == userId && (categoryId == null || e.CategoryId == categoryId))
        {
            AddInclude(e => e.Category);
            AddOrderByDescending(e => e.ExpenseDate);
            ApplyPaging(skip, take);
        }
    }

    public class ExpensesByUserSpec : BaseSpecification<Expense>
    {
        public ExpensesByUserSpec(int userId) : base(e => e.UserId == userId) { }
    }

    public class ExpensesByCategorySpec : BaseSpecification<Expense>
    {
        public ExpensesByCategorySpec(int userId, int categoryId)
            : base(e => e.UserId == userId && e.CategoryId == categoryId)
        {
            AddInclude(e => e.Category);
            AddOrderByDescending(e => e.ExpenseDate);
        }
    }

    public class RecentExpensesSpec : BaseSpecification<Expense>
    {
        public RecentExpensesSpec(int userId, int take = 10)
            : base(e => e.UserId == userId)
        {
            AddInclude(e => e.Category);
            AddOrderByDescending(e => e.CreatedAt);
            ApplyPaging(0, take);
        }
    }
}
