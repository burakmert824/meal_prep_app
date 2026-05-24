namespace MealPrepper.Core.Entities;

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid FoodId { get; set; }
    public decimal Quantity { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Food Food { get; set; } = null!;
}