using KitchenAssistant.Web.Services;
using WebRecipeService = KitchenAssistant.Web.Services.RecipeService;

namespace KitchenAssistant.Tests;

public class ShoppingListServiceTests
{
    private readonly ShoppingListService _sut;
    private readonly WebRecipeService _recipeService;

    public ShoppingListServiceTests()
    {
        _recipeService = new WebRecipeService();
        _sut = new ShoppingListService(_recipeService);
    }

    [Fact]
    public void InitialState_IsEmpty()
    {
        Assert.Equal(0, _sut.TotalCount);
        Assert.Equal(0, _sut.CompletedCount);
    }

    [Fact]
    public void AddItem_NewItem_IncreasesCount()
    {
        _sut.AddItem("番茄", "2个");

        Assert.Equal(1, _sut.TotalCount);
    }

    [Fact]
    public void AddItem_DuplicateName_UpdatesQuantity()
    {
        _sut.AddItem("番茄", "2个");
        _sut.AddItem("番茄", "3个");

        Assert.Equal(1, _sut.TotalCount);
        Assert.Equal("3个", _sut.GetAllItems().First().Quantity);
    }

    [Fact]
    public void AddItem_EmptyName_IsIgnored()
    {
        _sut.AddItem(string.Empty);
        _sut.AddItem("   ");

        Assert.Equal(0, _sut.TotalCount);
    }

    [Fact]
    public void ToggleItem_MarksAsPurchased()
    {
        _sut.AddItem("鸡蛋");
        var id = _sut.GetAllItems().First().Id;

        _sut.ToggleItem(id);

        Assert.Equal(1, _sut.CompletedCount);
    }

    [Fact]
    public void ToggleItem_TwiceRestoresUnpurchased()
    {
        _sut.AddItem("鸡蛋");
        var id = _sut.GetAllItems().First().Id;

        _sut.ToggleItem(id);
        _sut.ToggleItem(id);

        Assert.Equal(0, _sut.CompletedCount);
    }

    [Fact]
    public void RemoveItem_ExistingItem_DecreasesCount()
    {
        _sut.AddItem("鸡蛋");
        var id = _sut.GetAllItems().First().Id;

        _sut.RemoveItem(id);

        Assert.Equal(0, _sut.TotalCount);
    }

    [Fact]
    public void ClearCompleted_RemovesOnlyPurchased()
    {
        _sut.AddItem("鸡蛋");
        _sut.AddItem("番茄");
        var id = _sut.GetAllItems().First().Id;
        _sut.ToggleItem(id);

        _sut.ClearCompleted();

        Assert.Equal(1, _sut.TotalCount);
        Assert.Equal(0, _sut.CompletedCount);
    }

    [Fact]
    public void ClearAll_RemovesEverything()
    {
        _sut.AddItem("鸡蛋");
        _sut.AddItem("番茄");

        _sut.ClearAll();

        Assert.Equal(0, _sut.TotalCount);
    }

    [Fact]
    public void GenerateFromRecipe_AddsAllIngredients()
    {
        var recipe = _recipeService.GetAllRecipes().First();

        _sut.GenerateFromRecipe(recipe.Id);

        Assert.True(_sut.TotalCount > 0);
    }

    [Fact]
    public void GenerateFromRecipe_UnknownId_DoesNothing()
    {
        _sut.GenerateFromRecipe(Guid.NewGuid());

        Assert.Equal(0, _sut.TotalCount);
    }

    [Fact]
    public void GetItemsByCategory_GroupsCorrectly()
    {
        _sut.AddItem("猪肉", "500g", "🥩 肉类");
        _sut.AddItem("白菜", "1棵", "🥬 蔬菜");

        var groups = _sut.GetItemsByCategory();

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, g => g.Key == "🥩 肉类");
        Assert.Contains(groups, g => g.Key == "🥬 蔬菜");
    }

    [Fact]
    public void OnChange_FiredOnAddItem()
    {
        var fired = false;
        _sut.OnChange += () => fired = true;

        _sut.AddItem("鸡蛋");

        Assert.True(fired);
    }

    [Fact]
    public void OnChange_FiredOnToggle()
    {
        _sut.AddItem("鸡蛋");
        var id = _sut.GetAllItems().First().Id;
        var fired = false;
        _sut.OnChange += () => fired = true;

        _sut.ToggleItem(id);

        Assert.True(fired);
    }

    [Fact]
    public void ToggleItem_SetsPurchasedAt_WhenMarked()
    {
        _sut.AddItem("鸡蛋");
        var id = _sut.GetAllItems().First().Id;

        _sut.ToggleItem(id);

        var item = _sut.GetAllItems().First();
        Assert.NotNull(item.PurchasedAt);
    }

    [Fact]
    public void ToggleItem_ClearsPurchasedAt_WhenUnmarked()
    {
        _sut.AddItem("鸡蛋");
        var id = _sut.GetAllItems().First().Id;
        _sut.ToggleItem(id);

        _sut.ToggleItem(id);

        var item = _sut.GetAllItems().First();
        Assert.Null(item.PurchasedAt);
    }
}
