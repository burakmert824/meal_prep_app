namespace MealPrepper.Core.Entities;

public class Food
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CaloriesPerUnit { get; set; }
    public decimal ProteinPerUnit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<ShoppingListItem> ShoppingListItems { get; set; } = [];
}
