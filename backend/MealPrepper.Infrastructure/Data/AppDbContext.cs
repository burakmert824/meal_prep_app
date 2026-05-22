using MealPrepper.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Food> Foods => Set<Food>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd();
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.HasMany(u => u.Foods).WithOne(f => f.User).HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Food>(e =>
        {
            e.ToTable("Foods");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).ValueGeneratedOnAdd();
            e.Property(f => f.Name).IsRequired().HasMaxLength(200);
            e.Property(f => f.Unit).IsRequired().HasMaxLength(50);
            e.Property(f => f.CaloriesPerUnit).HasColumnType("decimal(18,4)");
            e.HasIndex(f => new { f.UserId, f.Name });
        });
    }
}
