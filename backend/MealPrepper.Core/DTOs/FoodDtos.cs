namespace MealPrepper.Core.DTOs;

public record FoodDto(Guid Id, Guid UserId, string Name, string Unit, decimal CaloriesPerUnit);

public record CreateFoodDto(string Name, string Unit, decimal CaloriesPerUnit);

public record UpdateFoodDto(string Name, string Unit, decimal CaloriesPerUnit);
