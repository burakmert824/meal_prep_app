using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Services;

public class MealEntryService(AppDbContext db) : IMealEntryService
{
    /// <inheritdoc/>
    public async Task<List<MealEntryDto>> GetRangeAsync(Guid userId, DateTime from, DateTime to)
    {
        var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc);
        return await db.MealEntries
            .AsNoTracking()
            .Where(me => me.UserId == userId && me.Date >= fromUtc && me.Date <= toUtc)
            .Include(me => me.Recipe)
            .OrderBy(me => me.Date).ThenBy(me => me.MealSlot)
            .Select(me => new MealEntryDto(me.Id, me.UserId, me.RecipeId, me.Recipe.Name, me.Date, me.MealSlot, me.PortionMultiplier))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<MealEntryDto> CreateAsync(Guid userId, CreateMealEntryDto dto)
    {
        var recipe = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RecipeId && r.UserId == userId);
        if (recipe is null) throw new InvalidOperationException("Recipe not found.");

        var entry = new MealEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RecipeId = dto.RecipeId,
            Date = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc),
            MealSlot = dto.MealSlot,
            PortionMultiplier = dto.PortionMultiplier
        };

        db.MealEntries.Add(entry);
        await db.SaveChangesAsync();
        return new MealEntryDto(entry.Id, entry.UserId, entry.RecipeId, recipe.Name, entry.Date, entry.MealSlot, entry.PortionMultiplier);
    }

    /// <inheritdoc/>
    public async Task<MealEntryDto?> UpdateAsync(Guid userId, Guid id, UpdateMealEntryDto dto)
    {
        var entry = await db.MealEntries.Include(me => me.Recipe).FirstOrDefaultAsync(me => me.Id == id && me.UserId == userId);
        if (entry is null) return null;

        entry.PortionMultiplier = dto.PortionMultiplier;
        await db.SaveChangesAsync();
        return new MealEntryDto(entry.Id, entry.UserId, entry.RecipeId, entry.Recipe.Name, entry.Date, entry.MealSlot, entry.PortionMultiplier);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var entry = await db.MealEntries.FirstOrDefaultAsync(me => me.Id == id && me.UserId == userId);
        if (entry is null) return false;
        db.MealEntries.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }
}
