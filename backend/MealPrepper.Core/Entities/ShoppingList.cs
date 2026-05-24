namespace MealPrepper.Core.Entities;

public class ShoppingList
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public User User { get; set; } = null!;
    public ICollection<ShoppingListItem> Items { get; set; } = [];
}
