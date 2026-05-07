namespace KitchenAssistant.Core.Models;

public class ShoppingList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ShoppingItem> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

public class ShoppingItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Category { get; set; } = "其他";
    public bool IsPurchased { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
