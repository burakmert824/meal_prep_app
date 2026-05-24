namespace MealPrepper.Core.DTOs;

public record ShoppingListDto(Guid Id, Guid UserId, DateTime FromDate, DateTime ToDate, DateTime GeneratedAt, List<ShoppingListItemDto> Items);
public record ShoppingListItemDto(Guid Id, Guid FoodId, string FoodName, string Unit, decimal TotalQuantity, bool IsChecked);
public record GenerateShoppingListDto(DateTime FromDate, DateTime ToDate);
public record ToggleShoppingListItemDto(bool IsChecked);
