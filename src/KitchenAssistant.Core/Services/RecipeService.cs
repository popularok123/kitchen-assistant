using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Core.Services;

public class RecipeService
{
    private readonly List<Recipe> _builtInRecipes = [];
    private readonly List<Recipe> _customRecipes = [];
    private readonly HashSet<Guid> _favorites = [];

    public IReadOnlyList<Recipe> BuiltInRecipes => _builtInRecipes.AsReadOnly();
    public IReadOnlyList<Recipe> CustomRecipes => _customRecipes.AsReadOnly();

    public RecipeService()
    {
        SeedSampleRecipes();
    }

    public List<Recipe> GetAllRecipes() => _builtInRecipes.Concat(_customRecipes).ToList();

    public Recipe? GetRecipeById(Guid id) => GetAllRecipes().FirstOrDefault(r => r.Id == id);

    public void AddRecipe(Recipe recipe)
    {
        recipe.Id = Guid.NewGuid();
        recipe.CreatedAt = DateTime.Now;
        _customRecipes.Add(recipe);
    }

    public void AddCustomRecipes(IEnumerable<Recipe> recipes)
    {
        _customRecipes.Clear();
        _customRecipes.AddRange(recipes);
    }

    public void DeleteRecipe(Guid recipeId)
    {
        _customRecipes.RemoveAll(r => r.Id == recipeId);
    }

    public bool IsCustomRecipe(Guid recipeId) => _customRecipes.Any(r => r.Id == recipeId);

    public void ToggleFavorite(Guid recipeId)
    {
        if (_favorites.Contains(recipeId))
            _favorites.Remove(recipeId);
        else
            _favorites.Add(recipeId);
    }

    public bool IsFavorite(Guid recipeId) => _favorites.Contains(recipeId);

    public List<Guid> GetFavorites() => _favorites.ToList();

    public void SetFavorites(IEnumerable<Guid> favorites)
    {
        _favorites.Clear();
        foreach (var f in favorites) _favorites.Add(f);
    }

    public List<Recipe> GetFavoriteRecipes()
    {
        return GetAllRecipes().Where(r => _favorites.Contains(r.Id)).ToList();
    }

    public List<Recipe> SearchRecipes(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllRecipes();

        return GetAllRecipes().Where(r =>
            r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            r.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            r.Ingredients.Any(i => i.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public List<Recipe> FilterByDifficulty(int? minLevel, int? maxLevel)
    {
        var allRecipes = GetAllRecipes();
        var query = allRecipes.AsEnumerable();
        if (minLevel.HasValue)
            query = query.Where(r => r.DifficultyLevel >= minLevel.Value);
        if (maxLevel.HasValue)
            query = query.Where(r => r.DifficultyLevel <= maxLevel.Value);
        return query.ToList();
    }

    public List<Recipe> FilterByCookTime(int? maxMinutes)
    {
        if (!maxMinutes.HasValue)
            return GetAllRecipes();
        return GetAllRecipes().Where(r => r.CookTimeMinutes <= maxMinutes.Value).ToList();
    }

    public List<Recipe> FindRecipesByIngredient(string ingredient)
    {
        if (string.IsNullOrWhiteSpace(ingredient))
            return GetAllRecipes();

        return GetAllRecipes().Where(r =>
            r.Ingredients.Any(i => i.Contains(ingredient, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    private void SeedSampleRecipes()
    {
        _builtInRecipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "番茄炒蛋",
            Description = "经典家常菜，简单又美味",
            CookTimeMinutes = 15,
            DifficultyLevel = 1,
            Ingredients = { "鸡蛋 3个", "番茄 2个", "葱花 适量", "盐 适量", "糖 少许" },
            Steps =
            {
                new CookingStep { Order = 1, Instruction = "鸡蛋打散，加少许盐搅拌均匀", DurationSeconds = 60 },
                new CookingStep { Order = 2, Instruction = "番茄洗净切块", DurationSeconds = 120 },
                new CookingStep { Order = 3, Instruction = "热锅凉油，倒入蛋液，炒至凝固盛出", DurationSeconds = 180 },
                new CookingStep { Order = 4, Instruction = "锅中加少许油，放入番茄翻炒出汁", DurationSeconds = 150 },
                new CookingStep { Order = 5, Instruction = "加入炒好的鸡蛋，加盐和少许糖调味", DurationSeconds = 60 },
                new CookingStep { Order = 6, Instruction = "撒上葱花，出锅装盘", DurationSeconds = 30 }
            },
            Tips = { "番茄炒软出汁更好吃", "鸡蛋不要炒太老", "可以加番茄酱增加风味" },
            CreatedAt = DateTime.Now
        });

        _builtInRecipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "红烧肉",
            Description = "肥而不腻，入口即化的经典红烧肉",
            CookTimeMinutes = 90,
            DifficultyLevel = 3,
            Ingredients = { "五花肉 500g", "冰糖 30g", "生抽 2勺", "老抽 1勺", "料酒 2勺", "姜片 适量", "八角 2个" },
            Steps =
            {
                new CookingStep { Order = 1, Instruction = "五花肉切块，冷水下锅焯水，捞出洗净", DurationSeconds = 600 },
                new CookingStep { Order = 2, Instruction = "锅中放少许油，小火炒冰糖至琥珀色", DurationSeconds = 120 },
                new CookingStep { Order = 3, Instruction = "放入五花肉翻炒上色", DurationSeconds = 180 },
                new CookingStep { Order = 4, Instruction = "加入姜片、八角、料酒、生抽、老抽翻炒", DurationSeconds = 60 },
                new CookingStep { Order = 5, Instruction = "加入热水没过肉，大火烧开后小火炖60分钟", DurationSeconds = 3600 },
                new CookingStep { Order = 6, Instruction = "大火收汁，汤汁浓稠即可出锅", DurationSeconds = 300 }
            },
            Tips = { "焯水时加料酒去腥", "一定要加热水，否则肉会柴", "收汁时不停翻炒防止糊底" },
            CreatedAt = DateTime.Now
        });
    }
}
