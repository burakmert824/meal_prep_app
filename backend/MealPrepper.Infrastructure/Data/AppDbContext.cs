using MealPrepper.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd();
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.HasMany(u => u.Foods).WithOne(f => f.User).HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.MealEntries).WithOne(me => me.User).HasForeignKey(me => me.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(u => u.ShoppingList).WithOne(sl => sl.User).HasForeignKey<ShoppingList>(sl => sl.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Food>(e =>
        {
            e.ToTable("Foods");
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).ValueGeneratedOnAdd();
            e.Property(f => f.Name).IsRequired().HasMaxLength(200);
            e.Property(f => f.Unit).IsRequired().HasMaxLength(50);
            e.Property(f => f.CaloriesPerUnit).HasColumnType("decimal(18,4)");
            e.Property(f => f.ProteinPerUnit).HasColumnType("decimal(18,4)");
            e.HasIndex(f => new { f.UserId, f.Name });
        });

        modelBuilder.Entity<Recipe>(e =>
        {
            e.ToTable("Recipes");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd();
            e.Property(r => r.Name).IsRequired().HasMaxLength(200);
            e.Property(r => r.DefaultPortionSize).HasColumnType("decimal(18,4)");
            e.HasIndex(r => new { r.UserId, r.Name });
            e.HasMany(r => r.RecipeIngredients).WithOne(ri => ri.Recipe).HasForeignKey(ri => ri.RecipeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(r => r.MealEntries).WithOne(me => me.Recipe).HasForeignKey(me => me.RecipeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecipeIngredient>(e =>
        {
            e.ToTable("RecipeIngredients");
            e.HasKey(ri => ri.Id);
            e.Property(ri => ri.Id).ValueGeneratedOnAdd();
            e.Property(ri => ri.Quantity).HasColumnType("decimal(18,4)");
            e.HasOne(ri => ri.Food).WithMany().HasForeignKey(ri => ri.FoodId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MealEntry>(e =>
        {
            e.ToTable("MealEntries");
            e.HasKey(me => me.Id);
            e.Property(me => me.Id).ValueGeneratedOnAdd();
            e.Property(me => me.Date).IsRequired();
            e.Property(me => me.MealSlot).HasConversion<int>();
            e.Property(me => me.PortionMultiplier).HasColumnType("decimal(10,2)");
            e.HasIndex(me => new { me.UserId, me.Date });
        });

        modelBuilder.Entity<ShoppingList>(e =>
        {
            e.ToTable("ShoppingLists");
            e.HasKey(sl => sl.Id);
            e.Property(sl => sl.Id).ValueGeneratedOnAdd();
            e.Property(sl => sl.FromDate).IsRequired();
            e.Property(sl => sl.ToDate).IsRequired();
            e.HasIndex(sl => sl.UserId).IsUnique();
        });

        modelBuilder.Entity<ShoppingListItem>(e =>
        {
            e.ToTable("ShoppingListItems");
            e.HasKey(sli => sli.Id);
            e.Property(sli => sli.Id).ValueGeneratedOnAdd();
            e.Property(sli => sli.TotalQuantity).HasColumnType("decimal(18,4)");
            e.HasOne(sli => sli.ShoppingList).WithMany(sl => sl.Items).HasForeignKey(sli => sli.ShoppingListId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(sli => sli.Food).WithMany(f => f.ShoppingListItems).HasForeignKey(sli => sli.FoodId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
