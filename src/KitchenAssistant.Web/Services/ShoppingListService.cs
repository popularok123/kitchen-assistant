using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Web.Services;

public class ShoppingListService
{
    private readonly List<ShoppingItem> _items = new();
    private readonly RecipeService _recipeService;

    public event Action? OnChange;

    public ShoppingListService(RecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    public IReadOnlyList<ShoppingItem> GetAllItems() => _items.AsReadOnly();

    public int TotalCount => _items.Count;
    public int CompletedCount => _items.Count(i => i.IsPurchased);

    public void AddItem(string name, string quantity = "", string category = "其他")
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var existing = _items.FirstOrDefault(i =>
            i.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(quantity))
                existing.Quantity = quantity;
        }
        else
        {
            _items.Add(new ShoppingItem
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Quantity = quantity,
                Category = category,
                IsPurchased = false,
                AddedAt = DateTime.Now
            });
        }
        NotifyStateChanged();
    }

    public void ToggleItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.IsPurchased = !item.IsPurchased;
            item.PurchasedAt = item.IsPurchased ? DateTime.Now : null;
            NotifyStateChanged();
        }
    }

    public void RemoveItem(Guid itemId)
    {
        _items.RemoveAll(i => i.Id == itemId);
        NotifyStateChanged();
    }

    public void ClearCompleted()
    {
        _items.RemoveAll(i => i.IsPurchased);
        NotifyStateChanged();
    }

    public void ClearAll()
    {
        _items.Clear();
        NotifyStateChanged();
    }

    public void GenerateFromRecipe(Guid recipeId)
    {
        var recipe = _recipeService.GetRecipeById(recipeId);
        if (recipe == null) return;

        foreach (var ingredient in recipe.Ingredients)
        {
            var (name, quantity, category) = ParseIngredient(ingredient);
            AddItem(name, quantity, category);
        }
    }

    public List<IGrouping<string, ShoppingItem>> GetItemsByCategory()
    {
        return _items.GroupBy(i => i.Category).OrderBy(g => g.Key).ToList();
    }

    private static (string name, string quantity, string category) ParseIngredient(string ingredient)
    {
        var parts = ingredient.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string name = ingredient;
        string quantity = "";
        string category = "其他";

        if (parts.Length >= 2 && char.IsDigit(parts[0][0]))
        {
            quantity = parts[0];
            name = parts[1];
        }

        var vegetableKeywords = new[] { "菜", "瓜", "椒", "葱", "姜", "蒜", "番茄", "土豆", "萝卜", "白菜", "菇", "芹", "茄", "笋", "豆" };
        var meatKeywords = new[] { "肉", "鸡", "鸭", "鱼", "牛", "猪", "羊", "排", "骨", "虾", "蟹" };
        var seasoningKeywords = new[] { "盐", "糖", "油", "酱", "醋", "椒", "粉", "料", "味", "香" };

        if (vegetableKeywords.Any(k => name.Contains(k)))
            category = "🥬 蔬菜";
        else if (meatKeywords.Any(k => name.Contains(k)))
            category = "🥩 肉类";
        else if (seasoningKeywords.Any(k => name.Contains(k)))
            category = "🧂 调料";
        else if (name.Contains("蛋") || name.Contains("奶"))
            category = "🥚 蛋奶";
        else if (name.Contains("米") || name.Contains("面"))
            category = "🍚 主食";

        return (name, quantity, category);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
