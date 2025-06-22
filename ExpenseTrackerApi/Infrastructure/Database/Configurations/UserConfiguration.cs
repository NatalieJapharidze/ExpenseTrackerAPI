using ExpenseTrackerApi.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.Database.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255);
            builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}
