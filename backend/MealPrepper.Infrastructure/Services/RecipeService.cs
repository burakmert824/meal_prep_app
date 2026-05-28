using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Services;

public class RecipeService(AppDbContext db) : IRecipeService
{
    public async Task<IEnumerable<RecipeDto>> GetByUserAsync(Guid userId, string? search)
    {
        var query = db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.ToLower().Contains(search.ToLower()));

        return await query.OrderBy(r => r.Name).Select(r => ToDto(r)).ToListAsync();
    }

    public async Task<RecipeDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var recipe = await db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        return recipe is null ? null : ToDto(recipe);
    }

    public async Task<RecipeDto> CreateAsync(Guid userId, CreateRecipeDto dto)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            DefaultPortionSize = dto.DefaultPortionSize,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RecipeIngredients = dto.Ingredients.Select(i => new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                FoodId = i.FoodId,
                Quantity = i.Quantity
            }).ToList()
        };

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        await db.Entry(recipe).Collection(r => r.RecipeIngredients).Query()
            .Include(ri => ri.Food).LoadAsync();

        return ToDto(recipe);
    }

    public async Task<RecipeDto?> UpdateAsync(Guid userId, Guid id, UpdateRecipeDto dto)
    {
        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (recipe is null) return null;

        recipe.Name = dto.Name.Trim();
        recipe.DefaultPortionSize = dto.DefaultPortionSize;
        recipe.UpdatedAt = DateTime.UtcNow;

        db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
        db.RecipeIngredients.AddRange(dto.Ingredients.Select(i => new RecipeIngredient
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            FoodId = i.FoodId,
            Quantity = i.Quantity
        }));

        await db.SaveChangesAsync();

        await db.Entry(recipe).Collection(r => r.RecipeIngredients).Query()
            .Include(ri => ri.Food).LoadAsync();

        return ToDto(recipe);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (recipe is null) return false;
        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync();
        return true;
    }

    private static RecipeDto ToDto(Recipe r) => new(
        r.Id, r.UserId, r.Name, r.DefaultPortionSize,
        r.RecipeIngredients.Select(ri => new RecipeIngredientDto(
            ri.Id, ri.FoodId, ri.Food.Name, ri.Food.Unit, ri.Quantity)).ToList());
}