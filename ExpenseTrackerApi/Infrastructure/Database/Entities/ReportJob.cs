namespace ExpenseTrackerApi.Infrastructure.Database.Entities
{
    public class ReportJob
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public User User { get; set; } = null!;
    }
}
