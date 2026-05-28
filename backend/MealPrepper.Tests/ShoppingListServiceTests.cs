using FluentAssertions;
using MealPrepper.Core.Entities;
using MealPrepper.Infrastructure.Data;
using MealPrepper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Tests;

public class ShoppingListServiceTests
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

    private static Food SeedFood(AppDbContext db, Guid userId, string name = "Chicken")
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

    private static Recipe SeedRecipe(AppDbContext db, Guid userId, Food food, decimal ingredientQty = 200m, decimal defaultPortionSize = 1m)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Recipe",
            DefaultPortionSize = defaultPortionSize,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients =
            [
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    FoodId = food.Id,
                    Quantity = ingredientQty
                }
            ]
        };
        db.Recipes.Add(recipe);
        db.SaveChanges();
        return recipe;
    }

    private static MealEntry SeedMealEntry(AppDbContext db, Guid userId, Guid recipeId, DateTime date, decimal portionMultiplier = 1m)
    {
        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RecipeId = recipeId,
            Date = date.Date,
            MealSlot = MealSlot.Lunch,
            PortionMultiplier = portionMultiplier
        };
        db.MealEntries.Add(entry);
        db.SaveChanges();
        return entry;
    }

    // --------------- GetAsync ---------------

    [Fact]
    public async Task GetAsync_NoListExists_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new ShoppingListService(db);

        var result = await sut.GetAsync(user.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ListExists_ReturnsListWithItems()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Broccoli");
        var recipe = SeedRecipe(db, user.Id, food, 300m);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);
        await sut.GenerateAsync(user.Id, from, to);

        var result = await sut.GetAsync(user.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().FoodName.Should().Be("Broccoli");
    }

    [Fact]
    public async Task GetAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id);
        var recipe = SeedRecipe(db, user1.Id, food);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user1.Id, recipe.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);
        await sut.GenerateAsync(user1.Id, from, to);

        var result = await sut.GetAsync(user2.Id);

        result.Should().BeNull();
    }

    // --------------- GenerateAsync ---------------

    [Fact]
    public async Task GenerateAsync_SingleEntryWithIngredient_CorrectTotalQuantity()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Rice");
        // Recipe has 200g of Rice, entry portionMultiplier = 2
        // Expected total = 200 * 2 = 400
        var recipe = SeedRecipe(db, user.Id, food, ingredientQty: 200m);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc), portionMultiplier: 2m);
        var sut = new ShoppingListService(db);

        var result = await sut.GenerateAsync(user.Id, from, to);

        result.UserId.Should().Be(user.Id);
        result.Items.Should().HaveCount(1);
        result.Items.First().TotalQuantity.Should().Be(400m);
        result.Items.First().FoodName.Should().Be("Rice");
        result.Items.First().IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_TwoEntriesSameFoodDifferentDays_QuantitiesSummed()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Oats");
        // Each entry: 100g * 1 portion = 100g; two entries = 200g total
        var recipe = SeedRecipe(db, user.Id, food, ingredientQty: 100m);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);

        var result = await sut.GenerateAsync(user.Id, from, to);

        result.Items.Should().HaveCount(1);
        result.Items.First().TotalQuantity.Should().Be(200m);
    }

    [Fact]
    public async Task GenerateAsync_TwoEntriesDifferentFoods_TwoSeparateItems()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food1 = SeedFood(db, user.Id, "Chicken");
        var food2 = SeedFood(db, user.Id, "Spinach");
        var recipe1 = SeedRecipe(db, user.Id, food1, ingredientQty: 150m);
        var recipe2 = SeedRecipe(db, user.Id, food2, ingredientQty: 80m);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe1.Id, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        SeedMealEntry(db, user.Id, recipe2.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);

        var result = await sut.GenerateAsync(user.Id, from, to);

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.FoodName).Should().Contain("Chicken").And.Contain("Spinach");
    }

    [Fact]
    public async Task GenerateAsync_EntryOutsideRange_NotIncluded()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Tuna");
        var recipe = SeedRecipe(db, user.Id, food, ingredientQty: 120m);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        // Entry is outside the range
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);

        var result = await sut.GenerateAsync(user.Id, from, to);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_CalledTwice_ReplacesOldList()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Eggs");
        var recipe = SeedRecipe(db, user.Id, food, ingredientQty: 50m);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), portionMultiplier: 1m);
        var sut = new ShoppingListService(db);

        // First generation
        var first = await sut.GenerateAsync(user.Id, from, to);

        // Add second entry and regenerate
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc), portionMultiplier: 1m);
        var second = await sut.GenerateAsync(user.Id, from, to);

        // Only one shopping list per user
        var listsInDb = db.ShoppingLists.Where(sl => sl.UserId == user.Id).ToList();
        listsInDb.Should().HaveCount(1);

        // Second generation has updated total (50 + 50 = 100)
        second.Items.First().TotalQuantity.Should().Be(100m);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task GenerateAsync_StoresFromAndToDates()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        var sut = new ShoppingListService(db);

        var result = await sut.GenerateAsync(user.Id, from, to);

        result.FromDate.Date.Should().Be(from.Date);
        result.ToDate.Date.Should().Be(to.Date);
    }

    // --------------- ToggleItemAsync ---------------

    [Fact]
    public async Task ToggleItemAsync_ValidItem_FlipsIsCheckedToTrue()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Banana");
        var recipe = SeedRecipe(db, user.Id, food);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);
        var list = await sut.GenerateAsync(user.Id, from, to);
        var itemId = list.Items.First().Id;

        var result = await sut.ToggleItemAsync(user.Id, itemId, true);

        result.Should().NotBeNull();
        result!.IsChecked.Should().BeTrue();

        var inDb = await db.ShoppingListItems.FindAsync(itemId);
        inDb!.IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleItemAsync_ValidItem_FlipsIsCheckedToFalse()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Apple");
        var recipe = SeedRecipe(db, user.Id, food);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user.Id, recipe.Id, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);
        var list = await sut.GenerateAsync(user.Id, from, to);
        var itemId = list.Items.First().Id;
        // First set to true
        await sut.ToggleItemAsync(user.Id, itemId, true);

        // Then flip back to false
        var result = await sut.ToggleItemAsync(user.Id, itemId, false);

        result.Should().NotBeNull();
        result!.IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleItemAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id, "Milk");
        var recipe = SeedRecipe(db, user1.Id, food);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
        SeedMealEntry(db, user1.Id, recipe.Id, new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        var sut = new ShoppingListService(db);
        var list = await sut.GenerateAsync(user1.Id, from, to);
        var itemId = list.Items.First().Id;

        var result = await sut.ToggleItemAsync(user2.Id, itemId, true);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ToggleItemAsync_NonExistentItemId_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new ShoppingListService(db);

        var result = await sut.ToggleItemAsync(user.Id, Guid.NewGuid(), true);

        result.Should().BeNull();
    }
}
