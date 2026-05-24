namespace MealPrepper.Core.Entities;

public class MealEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime Date { get; set; }
    public MealSlot MealSlot { get; set; }
    public decimal PortionMultiplier { get; set; } = 1;
    public User User { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
