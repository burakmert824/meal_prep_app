using FluentAssertions;
using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Infrastructure.Data;
using MealPrepper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Tests;

public class MealEntryServiceTests
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

    private static Recipe SeedRecipe(AppDbContext db, Guid userId, string name = "Test Recipe")
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            DefaultPortionSize = 1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        db.SaveChanges();
        return recipe;
    }

    private static MealEntry SeedMealEntry(AppDbContext db, Guid userId, Guid recipeId, DateTime date, MealSlot slot = MealSlot.Lunch, decimal portionMultiplier = 1m)
    {
        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RecipeId = recipeId,
            Date = date.Date,
            MealSlot = slot,
            PortionMultiplier = portionMultiplier
        };
        db.MealEntries.Add(entry);
        db.SaveChanges();
        return entry;
    }

    // --------------- GetRangeAsync ---------------

    [Fact]
    public async Task GetRangeAsync_EntriesWithinRange_ReturnsOnlyThoseEntries()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var recipe = SeedRecipe(db, user.Id);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc));
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)); // outside range
        var sut = new MealEntryService(db);

        var result = await sut.GetRangeAsync(user.Id, from, to);

        result.Should().HaveCount(1);
        result.First().Date.Date.Should().Be(new DateTime(2026, 6, 3));
    }

    [Fact]
    public async Task GetRangeAsync_EntriesOnRangeBoundaries_AreIncluded()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var recipe = SeedRecipe(db, user.Id);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

        SeedMealEntry(db, user.Id, recipe.Id, from);
        SeedMealEntry(db, user.Id, recipe.Id, to);
        var sut = new MealEntryService(db);

        var result = await sut.GetRangeAsync(user.Id, from, to);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRangeAsync_WrongUser_ReturnsEmptyList()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var recipe = SeedRecipe(db, user1.Id);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user1.Id, recipe.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc));
        var sut = new MealEntryService(db);

        var result = await sut.GetRangeAsync(user2.Id, from, to);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRangeAsync_NoEntries_ReturnsEmptyList()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        var sut = new MealEntryService(db);

        var result = await sut.GetRangeAsync(user.Id, from, to);

        result.Should().BeEmpty();
    }

    // --------------- CreateAsync ---------------

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsEntryWithCorrectFields()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var recipe = SeedRecipe(db, user.Id, "Oatmeal");
        var sut = new MealEntryService(db);
        var date = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc);
        var dto = new CreateMealEntryDto(recipe.Id, date, MealSlot.Breakfast, 1.5m);

        var result = await sut.CreateAsync(user.Id, dto);

        result.UserId.Should().Be(user.Id);
        result.RecipeId.Should().Be(recipe.Id);
        result.RecipeName.Should().Be("Oatmeal");
        result.Date.Date.Should().Be(date.Date);
        result.MealSlot.Should().Be(MealSlot.Breakfast);
        result.PortionMultiplier.Should().Be(1.5m);

        var inDb = await db.MealEntries.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_RecipeNotOwnedByUser_ThrowsInvalidOperationException()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var recipe = SeedRecipe(db, user1.Id);
        var sut = new MealEntryService(db);
        var dto = new CreateMealEntryDto(recipe.Id, DateTime.UtcNow, MealSlot.Lunch, 1m);

        var act = async () => await sut.CreateAsync(user2.Id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(MealSlot.Breakfast)]
    [InlineData(MealSlot.Lunch)]
    [InlineData(MealSlot.Dinner)]
    [InlineData(MealSlot.Snack)]
    public async Task CreateAsync_AllMealSlots_PersistedCorrectly(MealSlot slot)
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var recipe = SeedRecipe(db, user.Id);
        var sut = new MealEntryService(db);
        var dto = new CreateMealEntryDto(recipe.Id, DateTime.UtcNow, slot, 1m);

        var result = await sut.CreateAsync(user.Id, dto);

        result.MealSlot.Should().Be(slot);
    }

    // --------------- UpdateAsync ---------------

    [Fact]
    public async Task UpdateAsync_OwnedEntry_UpdatesPortionMultiplier()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var recipe = SeedRecipe(db, user.Id);
        var entry = SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), portionMultiplier: 1m);
        var sut = new MealEntryService(db);
        var dto = new UpdateMealEntryDto(2.5m);

        var result = await sut.UpdateAsync(user.Id, entry.Id, dto);

        result.Should().NotBeNull();
        result!.PortionMultiplier.Should().Be(2.5m);
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var recipe = SeedRecipe(db, user1.Id);
        var entry = SeedMealEntry(db, user1.Id, recipe.Id, DateTime.UtcNow);
        var sut = new MealEntryService(db);
        var dto = new UpdateMealEntryDto(3m);

        var result = await sut.UpdateAsync(user2.Id, entry.Id, dto);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new MealEntryService(db);

        var result = await sut.UpdateAsync(user.Id, Guid.NewGuid(), new UpdateMealEntryDto(1m));

        result.Should().BeNull();
    }

    // --------------- DeleteAsync ---------------

    [Fact]
    public async Task DeleteAsync_OwnedEntry_RemovesEntryAndReturnsTrue()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var recipe = SeedRecipe(db, user.Id);
        var entry = SeedMealEntry(db, user.Id, recipe.Id, DateTime.UtcNow);
        var sut = new MealEntryService(db);

        var result = await sut.DeleteAsync(user.Id, entry.Id);

        result.Should().BeTrue();
        var inDb = await db.MealEntries.FindAsync(entry.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ReturnsFalse()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var recipe = SeedRecipe(db, user1.Id);
        var entry = SeedMealEntry(db, user1.Id, recipe.Id, DateTime.UtcNow);
        var sut = new MealEntryService(db);

        var result = await sut.DeleteAsync(user2.Id, entry.Id);

        result.Should().BeFalse();
        var inDb = await db.MealEntries.FindAsync(entry.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new MealEntryService(db);

        var result = await sut.DeleteAsync(user.Id, Guid.NewGuid());

        result.Should().BeFalse();
    }
}
