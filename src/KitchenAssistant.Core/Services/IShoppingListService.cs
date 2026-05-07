using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Core.Services;

public interface IShoppingListService
{
    ShoppingList GetCurrentList();
    ShoppingList CreateNewList(string name);
    void AddItem(string name, string quantity = "", string category = "其他");
    void RemoveItem(Guid itemId);
    void ToggleItem(Guid itemId);
    void UpdateItem(Guid itemId, string name, string quantity, string category);
    ShoppingList GenerateFromRecipe(Guid recipeId);
    ShoppingList GenerateFromRecipes(IEnumerable<Guid> recipeIds);
    void ClearCompleted();
    void ClearAll();
}
