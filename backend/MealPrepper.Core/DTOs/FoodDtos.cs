namespace MealPrepper.Core.DTOs;

public record FoodDto(Guid Id, Guid UserId, string Name, string Unit, decimal CaloriesPerUnit, decimal ProteinPerUnit);

public record CreateFoodDto(string Name, string Unit, decimal CaloriesPerUnit, decimal ProteinPerUnit);

public record UpdateFoodDto(string Name, string Unit, decimal CaloriesPerUnit, decimal ProteinPerUnit);
