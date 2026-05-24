namespace MealPrepper.Core.Entities;

public class ShoppingListItem
{
    public Guid Id { get; set; }
    public Guid ShoppingListId { get; set; }
    public Guid FoodId { get; set; }
    public decimal TotalQuantity { get; set; }
    public bool IsChecked { get; set; }

    public ShoppingList ShoppingList { get; set; } = null!;
    public Food Food { get; set; } = null!;
}
