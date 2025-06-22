using ExpenseTrackerApi.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.Database.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(50);
            builder.Property(x => x.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
            builder.Property(x => x.MonthlyBudget).HasColumnName("monthly_budget").HasColumnType("decimal(18,2)");
            builder.Property(x => x.IsActive).HasColumnName("is_active");

            builder.HasOne(x => x.User).WithMany(x => x.Categories).HasForeignKey(x => x.UserId);
        }
    }
}
