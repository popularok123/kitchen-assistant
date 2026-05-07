using System.Text.Json;
using KitchenAssistant.Core.Models;
using KitchenAssistant.Core.Services;

namespace KitchenAssistant.Client.Services;

public class RecipeStorageService
{
    private readonly string _storagePath;
    private readonly RecipeService _recipeService;
    private readonly List<Recipe> _customRecipes = [];
    private readonly HashSet<Guid> _favorites = [];

    public IReadOnlyList<Recipe> CustomRecipes => _customRecipes.AsReadOnly();
    public IReadOnlySet<Guid> Favorites => _favorites;

    public RecipeStorageService(RecipeService recipeService)
    {
        _recipeService = recipeService;
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KitchenAssistant");
        Directory.CreateDirectory(appDataPath);
        _storagePath = Path.Combine(appDataPath, "recipes.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_storagePath)) return;

        try
        {
            var json = File.ReadAllText(_storagePath);
            var data = JsonSerializer.Deserialize<RecipeStorageData>(json);
            if (data != null)
            {
                _customRecipes.Clear();
                _customRecipes.AddRange(data.CustomRecipes ?? []);
                _recipeService.AddCustomRecipes(_customRecipes);
                if (data.Favorites != null)
                {
                    _recipeService.SetFavorites(data.Favorites);
                }
            }
        }
        catch
        {
        }
    }

    public void Save()
    {
        var data = new RecipeStorageData
        {
            CustomRecipes = _recipeService.CustomRecipes.ToList(),
            Favorites = _recipeService.GetFavorites()
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_storagePath, json);
    }

    public void AddRecipe(Recipe recipe)
    {
        recipe.Id = Guid.NewGuid();
        recipe.CreatedAt = DateTime.Now;
        _customRecipes.Add(recipe);
        Save();
    }

    public void DeleteRecipe(Guid recipeId)
    {
        _customRecipes.RemoveAll(r => r.Id == recipeId);
        _favorites.Remove(recipeId);
        Save();
    }

    public void ToggleFavorite(Guid recipeId)
    {
        if (_favorites.Contains(recipeId))
            _favorites.Remove(recipeId);
        else
            _favorites.Add(recipeId);
        Save();
    }

    public bool IsFavorite(Guid recipeId) => _favorites.Contains(recipeId);

    private class RecipeStorageData
    {
        public List<Recipe>? CustomRecipes { get; set; }
        public List<Guid>? Favorites { get; set; }
    }
}
