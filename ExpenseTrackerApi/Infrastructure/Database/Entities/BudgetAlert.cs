namespace ExpenseTrackerApi.Infrastructure.Database.Entities
{
    public class BudgetAlert
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public string Month { get; set; } = string.Empty;
        public decimal PercentageUsed { get; set; }
        public DateTime AlertSentAt { get; set; }

        public User User { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}
