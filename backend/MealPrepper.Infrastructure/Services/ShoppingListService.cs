using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Services;

public class ShoppingListService(AppDbContext db) : IShoppingListService
{
    /// <inheritdoc/>
    public async Task<ShoppingListDto?> GetAsync(Guid userId)
    {
        var list = await db.ShoppingLists
            .AsNoTracking()
            .Include(sl => sl.Items).ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(sl => sl.UserId == userId);

        return list is null ? null : ToDto(list);
    }

    /// <inheritdoc/>
    public async Task<ShoppingListDto> GenerateAsync(Guid userId, DateTime from, DateTime to)
    {
        // Load all meal entries in range with their recipe ingredients
        var entries = await db.MealEntries
            .AsNoTracking()
            .Where(me => me.UserId == userId && me.Date.Date >= from.Date && me.Date.Date <= to.Date)
            .Include(me => me.Recipe).ThenInclude(r => r.RecipeIngredients).ThenInclude(ri => ri.Food)
            .ToListAsync();

        // Aggregate quantities by food
        var totals = new Dictionary<Guid, (Food Food, decimal Qty)>();
        foreach (var entry in entries)
        {
            foreach (var ing in entry.Recipe.RecipeIngredients)
            {
                var qty = ing.Quantity * entry.PortionMultiplier;
                if (totals.TryGetValue(ing.FoodId, out var existing))
                    totals[ing.FoodId] = (existing.Food, existing.Qty + qty);
                else
                    totals[ing.FoodId] = (ing.Food, qty);
            }
        }

        // Delete existing shopping list for this user (one per user)
        var existing2 = await db.ShoppingLists.Include(sl => sl.Items).FirstOrDefaultAsync(sl => sl.UserId == userId);
        if (existing2 is not null) db.ShoppingLists.Remove(existing2);

        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FromDate = from.Date,
            ToDate = to.Date,
            GeneratedAt = DateTime.UtcNow,
            Items = totals.Select(kv => new ShoppingListItem
            {
                Id = Guid.NewGuid(),
                FoodId = kv.Key,
                TotalQuantity = kv.Value.Qty,
                IsChecked = false
            }).ToList()
        };

        db.ShoppingLists.Add(list);
        await db.SaveChangesAsync();

        // Reload with Food navigation for DTO
        await db.Entry(list).Collection(sl => sl.Items).Query().Include(i => i.Food).LoadAsync();
        return ToDto(list);
    }

    /// <inheritdoc/>
    public async Task<ShoppingListItemDto?> ToggleItemAsync(Guid userId, Guid itemId, bool isChecked)
    {
        var item = await db.ShoppingListItems
            .Include(i => i.ShoppingList)
            .Include(i => i.Food)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ShoppingList.UserId == userId);

        if (item is null) return null;
        item.IsChecked = isChecked;
        await db.SaveChangesAsync();
        return new ShoppingListItemDto(item.Id, item.FoodId, item.Food.Name, item.Food.Unit, item.TotalQuantity, item.IsChecked);
    }

    private static ShoppingListDto ToDto(ShoppingList list) => new(
        list.Id, list.UserId, list.FromDate, list.ToDate, list.GeneratedAt,
        list.Items.Select(i => new ShoppingListItemDto(i.Id, i.FoodId, i.Food.Name, i.Food.Unit, i.TotalQuantity, i.IsChecked)).ToList());
}
