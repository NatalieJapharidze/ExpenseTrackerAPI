namespace ExpenseTrackerApi.Infrastructure.Database.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public decimal MonthlyBudget { get; set; }
        public bool IsActive { get; set; } = true;
        public User User { get; set; } = null!;
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
