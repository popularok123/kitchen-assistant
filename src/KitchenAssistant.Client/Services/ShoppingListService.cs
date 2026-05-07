using KitchenAssistant.Core.Models;
using KitchenAssistant.Core.Services;

namespace KitchenAssistant.Client.Services;

public class ShoppingListService : IShoppingListService
{
    private ShoppingList _currentList;
    private readonly RecipeService _recipeService;

    public ShoppingListService(RecipeService recipeService)
    {
        _recipeService = recipeService;
        _currentList = new ShoppingList
        {
            Id = Guid.NewGuid(),
            Name = "购物清单",
            CreatedAt = DateTime.Now
        };
    }

    public ShoppingList GetCurrentList() => _currentList;

    public ShoppingList CreateNewList(string name)
    {
        _currentList = new ShoppingList
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.Now
        };
        return _currentList;
    }

    public void AddItem(string name, string quantity = "", string category = "其他")
    {
        var existing = _currentList.Items.FirstOrDefault(i =>
            i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            if (!string.IsNullOrEmpty(quantity))
                existing.Quantity = quantity;
        }
        else
        {
            _currentList.Items.Add(new ShoppingItem
            {
                Id = Guid.NewGuid(),
                Name = name,
                Quantity = quantity,
                Category = category
            });
        }

        _currentList.LastUpdatedAt = DateTime.Now;
    }

    public void RemoveItem(Guid itemId)
    {
        _currentList.Items.RemoveAll(i => i.Id == itemId);
        _currentList.LastUpdatedAt = DateTime.Now;
    }

    public void ToggleItem(Guid itemId)
    {
        var item = _currentList.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.IsPurchased = !item.IsPurchased;
            item.PurchasedAt = item.IsPurchased ? DateTime.Now : null;
            _currentList.LastUpdatedAt = DateTime.Now;
        }
    }

    public void UpdateItem(Guid itemId, string name, string quantity, string category)
    {
        var item = _currentList.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Name = name;
            item.Quantity = quantity;
            item.Category = category;
            _currentList.LastUpdatedAt = DateTime.Now;
        }
    }

    public ShoppingList GenerateFromRecipe(Guid recipeId)
    {
        var recipe = _recipeService.GetRecipeById(recipeId);
        if (recipe == null) return _currentList;

        _currentList.Name = $"{recipe.Name} - 购物清单";

        foreach (var ingredient in recipe.Ingredients)
        {
            var (name, quantity, category) = ParseIngredient(ingredient);
            AddItem(name, quantity, category);
        }

        return _currentList;
    }

    public ShoppingList GenerateFromRecipes(IEnumerable<Guid> recipeIds)
    {
        _currentList.Name = "合并购物清单";
        _currentList.Items.Clear();

        foreach (var recipeId in recipeIds)
        {
            var recipe = _recipeService.GetRecipeById(recipeId);
            if (recipe != null)
            {
                foreach (var ingredient in recipe.Ingredients)
                {
                    var (name, quantity, category) = ParseIngredient(ingredient);
                    AddItem(name, quantity, category);
                }
            }
        }

        return _currentList;
    }

    public void ClearCompleted()
    {
        _currentList.Items.RemoveAll(i => i.IsPurchased);
        _currentList.LastUpdatedAt = DateTime.Now;
    }

    public void ClearAll()
    {
        _currentList.Items.Clear();
        _currentList.LastUpdatedAt = DateTime.Now;
    }

    private (string name, string quantity, string category) ParseIngredient(string ingredient)
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

        var vegetableKeywords = new[] { "菜", "瓜", "椒", "葱", "姜", "蒜", "番茄", "土豆", "萝卜", "白菜" };
        var meatKeywords = new[] { "肉", "鸡", "鸭", "鱼", "牛", "猪", "羊", "排", "骨" };
        var seasoningKeywords = new[] { "盐", "糖", "油", "酱", "醋", "椒", "粉", "料" };

        if (vegetableKeywords.Any(k => name.Contains(k)))
            category = "蔬菜";
        else if (meatKeywords.Any(k => name.Contains(k)))
            category = "肉类";
        else if (seasoningKeywords.Any(k => name.Contains(k)))
            category = "调料";

        return (name, quantity, category);
    }
}
