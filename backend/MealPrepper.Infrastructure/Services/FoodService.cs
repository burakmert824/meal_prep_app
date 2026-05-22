using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Services;

public class FoodService(AppDbContext db) : IFoodService
{
    public async Task<IEnumerable<FoodDto>> GetByUserAsync(Guid userId, string? search)
    {
        var query = db.Foods.AsNoTracking().Where(f => f.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.ToLower().Contains(search.ToLower()));

        return await query
            .OrderBy(f => f.Name)
            .Select(f => new FoodDto(f.Id, f.UserId, f.Name, f.Unit, f.CaloriesPerUnit))
            .ToListAsync();
    }

    public async Task<FoodDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var food = await db.Foods.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        return food is null ? null : new FoodDto(food.Id, food.UserId, food.Name, food.Unit, food.CaloriesPerUnit);
    }

    public async Task<FoodDto> CreateAsync(Guid userId, CreateFoodDto dto)
    {
        var food = new Food
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Unit = dto.Unit.Trim(),
            CaloriesPerUnit = dto.CaloriesPerUnit,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Foods.Add(food);
        await db.SaveChangesAsync();
        return new FoodDto(food.Id, food.UserId, food.Name, food.Unit, food.CaloriesPerUnit);
    }

    public async Task<FoodDto?> UpdateAsync(Guid userId, Guid id, UpdateFoodDto dto)
    {
        var food = await db.Foods.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (food is null) return null;

        food.Name = dto.Name.Trim();
        food.Unit = dto.Unit.Trim();
        food.CaloriesPerUnit = dto.CaloriesPerUnit;
        food.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return new FoodDto(food.Id, food.UserId, food.Name, food.Unit, food.CaloriesPerUnit);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var food = await db.Foods.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (food is null) return false;

        db.Foods.Remove(food);
        await db.SaveChangesAsync();
        return true;
    }
}
