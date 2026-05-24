namespace MealPrepper.Core.DTOs;

public record RecipeIngredientDto(Guid Id, Guid FoodId, string FoodName, string Unit, decimal Quantity);

public record RecipeDto(Guid Id, Guid UserId, string Name, decimal DefaultPortionSize, List<RecipeIngredientDto> Ingredients);

public record RecipeIngredientInputDto(Guid FoodId, decimal Quantity);

public record CreateRecipeDto(string Name, decimal DefaultPortionSize, List<RecipeIngredientInputDto> Ingredients);

public record UpdateRecipeDto(string Name, decimal DefaultPortionSize, List<RecipeIngredientInputDto> Ingredients);