using ExpenseTrackerApi.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.Database.Configurations
{
    public class BudgetAlertConfiguration : IEntityTypeConfiguration<BudgetAlert>
    {
        public void Configure(EntityTypeBuilder<BudgetAlert> builder)
        {
            builder.ToTable("budget_alerts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.CategoryId).HasColumnName("category_id");
            builder.Property(x => x.Month).HasColumnName("month").HasMaxLength(7);
            builder.Property(x => x.PercentageUsed).HasColumnName("percentage_used").HasColumnType("decimal(5,2)");
            builder.Property(x => x.AlertSentAt).HasColumnName("alert_sent_at");

            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
        }
    }
}
