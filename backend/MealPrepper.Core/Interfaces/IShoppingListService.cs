using MealPrepper.Core.DTOs;

namespace MealPrepper.Core.Interfaces;

public interface IShoppingListService
{
    Task<ShoppingListDto?> GetAsync(Guid userId);
    Task<ShoppingListDto> GenerateAsync(Guid userId, DateTime from, DateTime to);
    Task<ShoppingListItemDto?> ToggleItemAsync(Guid userId, Guid itemId, bool isChecked);
}
