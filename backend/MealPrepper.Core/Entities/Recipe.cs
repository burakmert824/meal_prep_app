namespace MealPrepper.Core.Entities;

public class Recipe
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultPortionSize { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public ICollection<MealEntry> MealEntries { get; set; } = [];
}
