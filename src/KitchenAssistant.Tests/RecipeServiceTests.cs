using KitchenAssistant.Core.Services;
using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Tests;

public class RecipeServiceTests
{
    private readonly RecipeService _sut = new();

    [Fact]
    public void GetAllRecipes_ReturnsBuiltInRecipes()
    {
        var recipes = _sut.GetAllRecipes();

        Assert.NotEmpty(recipes);
        Assert.All(recipes, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
    }

    [Fact]
    public void GetRecipeById_ExistingId_ReturnsRecipe()
    {
        var id = _sut.GetAllRecipes().First().Id;

        var recipe = _sut.GetRecipeById(id);

        Assert.NotNull(recipe);
        Assert.Equal(id, recipe.Id);
    }

    [Fact]
    public void GetRecipeById_UnknownId_ReturnsNull()
    {
        var recipe = _sut.GetRecipeById(Guid.NewGuid());

        Assert.Null(recipe);
    }

    [Fact]
    public void SearchRecipes_MatchingName_ReturnsResults()
    {
        var results = _sut.SearchRecipes("番茄");

        Assert.NotEmpty(results);
        Assert.All(results, r =>
            Assert.True(r.Name.Contains("番茄") || r.Description.Contains("番茄") ||
                        r.Ingredients.Any(i => i.Contains("番茄"))));
    }

    [Fact]
    public void SearchRecipes_NoMatch_ReturnsEmpty()
    {
        var results = _sut.SearchRecipes("xyzNotExist999");

        Assert.Empty(results);
    }

    [Fact]
    public void SearchRecipes_EmptyTerm_ReturnsAll()
    {
        var all = _sut.GetAllRecipes();
        var results = _sut.SearchRecipes(string.Empty);

        Assert.Equal(all.Count, results.Count);
    }

    [Fact]
    public void ToggleFavorite_AddAndRemove_WorksCorrectly()
    {
        var id = _sut.GetAllRecipes().First().Id;

        Assert.False(_sut.IsFavorite(id));

        _sut.ToggleFavorite(id);
        Assert.True(_sut.IsFavorite(id));

        _sut.ToggleFavorite(id);
        Assert.False(_sut.IsFavorite(id));
    }

    [Fact]
    public void GetFavoriteRecipes_ReturnsOnlyFavorites()
    {
        var recipes = _sut.GetAllRecipes();
        var first = recipes[0].Id;
        var second = recipes[1].Id;

        _sut.ToggleFavorite(first);
        _sut.ToggleFavorite(second);

        var favorites = _sut.GetFavoriteRecipes();

        Assert.Equal(2, favorites.Count);
        Assert.Contains(favorites, r => r.Id == first);
        Assert.Contains(favorites, r => r.Id == second);
    }

    [Fact]
    public void AddRecipe_CustomRecipe_AppearsInGetAll()
    {
        var recipe = new Recipe { Name = "测试菜谱", DifficultyLevel = 1, CookTimeMinutes = 10 };

        _sut.AddRecipe(recipe);

        Assert.Contains(_sut.GetAllRecipes(), r => r.Name == "测试菜谱");
        Assert.Contains(_sut.CustomRecipes, r => r.Name == "测试菜谱");
    }

    [Fact]
    public void AddRecipe_SetsNewId()
    {
        var recipe = new Recipe { Name = "测试菜谱", Id = Guid.Empty };

        _sut.AddRecipe(recipe);

        Assert.NotEqual(Guid.Empty, recipe.Id);
    }

    [Fact]
    public void DeleteRecipe_CustomRecipe_RemovedFromAll()
    {
        var recipe = new Recipe { Name = "待删除" };
        _sut.AddRecipe(recipe);
        Assert.Contains(_sut.GetAllRecipes(), r => r.Name == "待删除");

        _sut.DeleteRecipe(recipe.Id);

        Assert.DoesNotContain(_sut.GetAllRecipes(), r => r.Name == "待删除");
    }

    [Fact]
    public void FilterByDifficulty_MaxLevel1_ReturnsOnlyEasy()
    {
        var results = _sut.FilterByDifficulty(null, 1);

        Assert.All(results, r => Assert.Equal(1, r.DifficultyLevel));
    }

    [Fact]
    public void FilterByCookTime_MaxTime20_ReturnsShortRecipes()
    {
        var results = _sut.FilterByCookTime(20);

        Assert.All(results, r => Assert.True(r.CookTimeMinutes <= 20));
    }

    [Fact]
    public void FindRecipesByIngredient_Exists_ReturnsMatches()
    {
        var results = _sut.FindRecipesByIngredient("鸡蛋");

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains(r.Ingredients, i => i.Contains("鸡蛋")));
    }

    [Fact]
    public void IsCustomRecipe_BuiltIn_ReturnsFalse()
    {
        var builtInId = _sut.BuiltInRecipes.First().Id;

        Assert.False(_sut.IsCustomRecipe(builtInId));
    }

    [Fact]
    public void IsCustomRecipe_Custom_ReturnsTrue()
    {
        var recipe = new Recipe { Name = "自定义菜谱" };
        _sut.AddRecipe(recipe);

        Assert.True(_sut.IsCustomRecipe(recipe.Id));
    }
}
