using MealPrepper.Core.Entities;

namespace MealPrepper.Core.DTOs;

public record MealEntryDto(Guid Id, Guid UserId, Guid RecipeId, string RecipeName, DateTime Date, MealSlot MealSlot, decimal PortionMultiplier);
public record CreateMealEntryDto(Guid RecipeId, DateTime Date, MealSlot MealSlot, decimal PortionMultiplier);
public record UpdateMealEntryDto(decimal PortionMultiplier);
