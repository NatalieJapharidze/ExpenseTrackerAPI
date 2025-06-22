using System.Collections.Generic;
using System.Reflection.Emit;
using ExpenseTrackerApi.Infrastructure.Database.Configurations;
using ExpenseTrackerApi.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerApi.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<BudgetAlert> BudgetAlerts { get; set; }
        public DbSet<ReportJob> ReportJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
            modelBuilder.ApplyConfiguration(new BudgetAlertConfiguration());
            modelBuilder.ApplyConfiguration(new ReportJobConfiguration());

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            base.OnConfiguring(optionsBuilder);
        }
    }
}
