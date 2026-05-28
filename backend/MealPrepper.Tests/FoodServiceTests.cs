using FluentAssertions;
using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Infrastructure.Data;
using MealPrepper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Tests;

public class FoodServiceTests
{
    private static (AppDbContext Db, SqliteConnection Connection) CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }

    private static User SeedUser(AppDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private static Food SeedFood(AppDbContext db, Guid userId, string name = "Chicken Breast")
    {
        var food = new Food
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Unit = "g",
            CaloriesPerUnit = 1.65m,
            ProteinPerUnit = 0.31m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Foods.Add(food);
        db.SaveChanges();
        return food;
    }

    // --------------- GetByUserAsync ---------------

    [Fact]
    public async Task GetByUserAsync_UserWithFoods_ReturnsOnlyThatUsersFood()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        SeedFood(db, user1.Id, "Oats");
        SeedFood(db, user2.Id, "Rice");
        var sut = new FoodService(db);

        var result = await sut.GetByUserAsync(user1.Id, null);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Oats");
    }

    [Fact]
    public async Task GetByUserAsync_NoFoodsForUser_ReturnsEmptyList()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new FoodService(db);

        var result = await sut.GetByUserAsync(user.Id, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserAsync_SearchMatchesSubstring_ReturnMatchingFoods()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        SeedFood(db, user.Id, "Chicken Breast");
        SeedFood(db, user.Id, "Brown Rice");
        var sut = new FoodService(db);

        var result = await sut.GetByUserAsync(user.Id, "chicken");

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Chicken Breast");
    }

    [Fact]
    public async Task GetByUserAsync_SearchIsCaseInsensitive_ReturnMatchingFoods()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        SeedFood(db, user.Id, "Salmon Fillet");
        var sut = new FoodService(db);

        var result = await sut.GetByUserAsync(user.Id, "SALMON");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUserAsync_SearchNoMatch_ReturnsEmptyList()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        SeedFood(db, user.Id, "Broccoli");
        var sut = new FoodService(db);

        var result = await sut.GetByUserAsync(user.Id, "xyz");

        result.Should().BeEmpty();
    }

    // --------------- GetByIdAsync ---------------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsFood()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Spinach");
        var sut = new FoodService(db);

        var result = await sut.GetByIdAsync(user.Id, food.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Spinach");
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id);
        var sut = new FoodService(db);

        var result = await sut.GetByIdAsync(user2.Id, food.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new FoodService(db);

        var result = await sut.GetByIdAsync(user.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    // --------------- CreateAsync ---------------

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsFoodWithCorrectFields()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new FoodService(db);
        var dto = new CreateFoodDto("  Avocado  ", " each ", 160m, 2m);

        var result = await sut.CreateAsync(user.Id, dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("Avocado");
        result.Unit.Should().Be("each");
        result.CaloriesPerUnit.Should().Be(160m);
        result.ProteinPerUnit.Should().Be(2m);
        result.UserId.Should().Be(user.Id);

        var inDb = await db.Foods.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }

    // --------------- UpdateAsync ---------------

    [Fact]
    public async Task UpdateAsync_OwnedFood_UpdatesAllFields()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Old Name");
        var sut = new FoodService(db);
        var dto = new UpdateFoodDto("New Name", "cup", 200m, 5m);

        var result = await sut.UpdateAsync(user.Id, food.Id, dto);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Unit.Should().Be("cup");
        result.CaloriesPerUnit.Should().Be(200m);
        result.ProteinPerUnit.Should().Be(5m);
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id);
        var sut = new FoodService(db);
        var dto = new UpdateFoodDto("Hacked", "kg", 1m, 1m);

        var result = await sut.UpdateAsync(user2.Id, food.Id, dto);

        result.Should().BeNull();
    }

    // --------------- DeleteAsync ---------------

    [Fact]
    public async Task DeleteAsync_OwnedFood_RemovesFoodAndReturnsTrue()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id);
        var sut = new FoodService(db);

        var result = await sut.DeleteAsync(user.Id, food.Id);

        result.Should().BeTrue();
        var inDb = await db.Foods.FindAsync(food.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ReturnsFalse()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id);
        var sut = new FoodService(db);

        var result = await sut.DeleteAsync(user2.Id, food.Id);

        result.Should().BeFalse();
        var inDb = await db.Foods.FindAsync(food.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new FoodService(db);

        var result = await sut.DeleteAsync(user.Id, Guid.NewGuid());

        result.Should().BeFalse();
    }
}
