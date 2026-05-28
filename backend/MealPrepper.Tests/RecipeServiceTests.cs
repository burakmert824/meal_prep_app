using FluentAssertions;
using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Infrastructure.Data;
using MealPrepper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Tests;

public class RecipeServiceTests
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

    private static Recipe SeedRecipe(AppDbContext db, Guid userId, Food food, string name = "Grilled Chicken")
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            DefaultPortionSize = 1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients =
            [
                new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    FoodId = food.Id,
                    Quantity = 200m
                }
            ]
        };
        db.Recipes.Add(recipe);
        db.SaveChanges();
        return recipe;
    }

    // --------------- GetByUserAsync ---------------

    [Fact]
    public async Task GetByUserAsync_UserWithRecipes_ReturnsOnlyThatUsersRecipes()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food1 = SeedFood(db, user1.Id);
        var food2 = SeedFood(db, user2.Id);
        SeedRecipe(db, user1.Id, food1, "Pasta");
        SeedRecipe(db, user2.Id, food2, "Sushi");
        var sut = new RecipeService(db);

        var result = await sut.GetByUserAsync(user1.Id, null);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Pasta");
    }

    [Fact]
    public async Task GetByUserAsync_RecipesIncludeIngredients_IngredientsPresent()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Rice");
        SeedRecipe(db, user.Id, food, "Rice Bowl");
        var sut = new RecipeService(db);

        var result = await sut.GetByUserAsync(user.Id, null);

        result.First().Ingredients.Should().HaveCount(1);
        result.First().Ingredients.First().FoodName.Should().Be("Rice");
    }

    [Fact]
    public async Task GetByUserAsync_SearchMatchesSubstring_ReturnsMatchingRecipe()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id);
        SeedRecipe(db, user.Id, food, "Chicken Stir Fry");
        SeedRecipe(db, user.Id, food, "Beef Burger");
        var sut = new RecipeService(db);

        var result = await sut.GetByUserAsync(user.Id, "chicken");

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Chicken Stir Fry");
    }

    [Fact]
    public async Task GetByUserAsync_NoRecipesForUser_ReturnsEmptyList()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new RecipeService(db);

        var result = await sut.GetByUserAsync(user.Id, null);

        result.Should().BeEmpty();
    }

    // --------------- GetByIdAsync ---------------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsRecipe()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id);
        var recipe = SeedRecipe(db, user.Id, food, "Omelette");
        var sut = new RecipeService(db);

        var result = await sut.GetByIdAsync(user.Id, recipe.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Omelette");
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id);
        var recipe = SeedRecipe(db, user1.Id, food);
        var sut = new RecipeService(db);

        var result = await sut.GetByIdAsync(user2.Id, recipe.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new RecipeService(db);

        var result = await sut.GetByIdAsync(user.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    // --------------- CreateAsync ---------------

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsRecipeAndIngredients()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id, "Egg");
        var sut = new RecipeService(db);
        var dto = new CreateRecipeDto(
            "  Scrambled Eggs  ",
            1m,
            [new RecipeIngredientInputDto(food.Id, 3m)]);

        var result = await sut.CreateAsync(user.Id, dto);

        result.Name.Should().Be("Scrambled Eggs");
        result.UserId.Should().Be(user.Id);
        result.DefaultPortionSize.Should().Be(1m);
        result.Ingredients.Should().HaveCount(1);
        result.Ingredients.First().FoodId.Should().Be(food.Id);
        result.Ingredients.First().Quantity.Should().Be(3m);

        var inDb = await db.Recipes.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_RecipeWithMultipleIngredients_AllIngredientsPersisted()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food1 = SeedFood(db, user.Id, "Tuna");
        var food2 = SeedFood(db, user.Id, "Pasta");
        var sut = new RecipeService(db);
        var dto = new CreateRecipeDto("Tuna Pasta", 2m,
        [
            new RecipeIngredientInputDto(food1.Id, 150m),
            new RecipeIngredientInputDto(food2.Id, 200m)
        ]);

        var result = await sut.CreateAsync(user.Id, dto);

        result.Ingredients.Should().HaveCount(2);
    }

    // --------------- UpdateAsync ---------------

    [Fact]
    public async Task UpdateAsync_OwnedRecipe_UpdatesNamePortionSizeAndIngredients()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food1 = SeedFood(db, user.Id, "OldFood");
        var food2 = SeedFood(db, user.Id, "NewFood");
        var recipe = SeedRecipe(db, user.Id, food1, "Old Recipe");
        var sut = new RecipeService(db);
        var dto = new UpdateRecipeDto("New Recipe", 3m,
            [new RecipeIngredientInputDto(food2.Id, 50m)]);

        var result = await sut.UpdateAsync(user.Id, recipe.Id, dto);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Recipe");
        result.DefaultPortionSize.Should().Be(3m);
        result.Ingredients.Should().HaveCount(1);
        result.Ingredients.First().FoodId.Should().Be(food2.Id);
        result.Ingredients.First().Quantity.Should().Be(50m);
    }

    [Fact]
    public async Task UpdateAsync_RemovesOldIngredientsAndAddsNew()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food1 = SeedFood(db, user.Id, "FoodA");
        var food2 = SeedFood(db, user.Id, "FoodB");
        var recipe = SeedRecipe(db, user.Id, food1);
        var sut = new RecipeService(db);

        // Update replaces food1 ingredient with food2 ingredient
        var dto = new UpdateRecipeDto(recipe.Name, recipe.DefaultPortionSize,
            [new RecipeIngredientInputDto(food2.Id, 100m)]);

        await sut.UpdateAsync(user.Id, recipe.Id, dto);

        var ingredients = db.RecipeIngredients.Where(ri => ri.RecipeId == recipe.Id).ToList();
        ingredients.Should().HaveCount(1);
        ingredients.First().FoodId.Should().Be(food2.Id);
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_ReturnsNull()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user1 = SeedUser(db);
        var user2 = SeedUser(db);
        var food = SeedFood(db, user1.Id);
        var recipe = SeedRecipe(db, user1.Id, food);
        var sut = new RecipeService(db);
        var dto = new UpdateRecipeDto("Hacked", 1m, []);

        var result = await sut.UpdateAsync(user2.Id, recipe.Id, dto);

        result.Should().BeNull();
    }

    // --------------- DeleteAsync ---------------

    [Fact]
    public async Task DeleteAsync_OwnedRecipe_RemovesRecipeAndReturnsTrue()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var food = SeedFood(db, user.Id);
        var recipe = SeedRecipe(db, user.Id, food);
        var sut = new RecipeService(db);

        var result = await sut.DeleteAsync(user.Id, recipe.Id);

        result.Should().BeTrue();
        var inDb = await db.Recipes.FindAsync(recipe.Id);
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
        var recipe = SeedRecipe(db, user1.Id, food);
        var sut = new RecipeService(db);

        var result = await sut.DeleteAsync(user2.Id, recipe.Id);

        result.Should().BeFalse();
        var inDb = await db.Recipes.FindAsync(recipe.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var (db, conn) = CreateDb();
        using var _ = conn;
        var user = SeedUser(db);
        var sut = new RecipeService(db);

        var result = await sut.DeleteAsync(user.Id, Guid.NewGuid());

        result.Should().BeFalse();
    }
}
