using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Web.Services;

public class RecipeService
{
    private readonly List<Recipe> _builtInRecipes = [];
    private readonly List<Recipe> _customRecipes = [];
    private readonly HashSet<Guid> _favoriteIds = [];

    public IReadOnlyList<Recipe> CustomRecipes => _customRecipes.AsReadOnly();

    public event Action? OnFavoritesChanged;

    public RecipeService()
    {
        SeedSampleRecipes();
    }

    public List<Recipe> GetAllRecipes() => [.. _builtInRecipes, .. _customRecipes];

    public Recipe? GetRecipeById(Guid id) => GetAllRecipes().FirstOrDefault(r => r.Id == id);

    public void AddRecipe(Recipe recipe)
    {
        if (recipe.Id == Guid.Empty)
            recipe.Id = Guid.NewGuid();
        recipe.CreatedAt = DateTime.Now;
        _customRecipes.Add(recipe);
        OnFavoritesChanged?.Invoke();
    }

    public void DeleteRecipe(Guid recipeId)
    {
        _customRecipes.RemoveAll(r => r.Id == recipeId);
        _favoriteIds.Remove(recipeId);
        OnFavoritesChanged?.Invoke();
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

    public bool IsFavorite(Guid recipeId) => _favoriteIds.Contains(recipeId);

    public void AddFavorite(Guid recipeId)
    {
        _favoriteIds.Add(recipeId);
        OnFavoritesChanged?.Invoke();
    }

    public void RemoveFavorite(Guid recipeId)
    {
        _favoriteIds.Remove(recipeId);
        OnFavoritesChanged?.Invoke();
    }

    public void ToggleFavorite(Guid recipeId)
    {
        if (IsFavorite(recipeId))
            RemoveFavorite(recipeId);
        else
            AddFavorite(recipeId);
    }

    public List<Recipe> GetFavoriteRecipes() => GetAllRecipes().Where(r => _favoriteIds.Contains(r.Id)).ToList();

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

        _builtInRecipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "宫保鸡丁",
            Description = "麻辣鲜香，经典川菜，下饭神器",
            CookTimeMinutes = 30,
            DifficultyLevel = 2,
            Ingredients = { "鸡胸肉 300g", "花生米 50g", "干辣椒 8个", "花椒 1茶匙", "葱 适量", "姜 适量", "蒜 适量", "生抽 2勺", "醋 1勺", "糖 1勺", "淀粉 适量" },
            Steps =
            {
                new CookingStep { Order = 1, Instruction = "鸡胸肉切丁，加盐、淀粉、料酒腌制15分钟", DurationSeconds = 900 },
                new CookingStep { Order = 2, Instruction = "调碗汁：生抽、醋、糖、淀粉、水混合均匀", DurationSeconds = 60 },
                new CookingStep { Order = 3, Instruction = "热锅下油，炒香花椒和干辣椒，捞出辣椒备用", DurationSeconds = 60 },
                new CookingStep { Order = 4, Instruction = "下鸡丁翻炒至变色，加葱姜蒜炒香", DurationSeconds = 180 },
                new CookingStep { Order = 5, Instruction = "倒入碗汁，大火翻炒均匀", DurationSeconds = 60 },
                new CookingStep { Order = 6, Instruction = "加入花生米和炒好的辣椒，翻炒出锅", DurationSeconds = 30 }
            },
            Tips = { "鸡丁腌制时加少量小苏打更嫩", "花生米提前炸好口感更脆", "碗汁可根据口味调整酸甜比例" },
            CreatedAt = DateTime.Now
        });

        _builtInRecipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "麻婆豆腐",
            Description = "麻辣嫩滑，豆腐与肉末完美结合",
            CookTimeMinutes = 20,
            DifficultyLevel = 2,
            Ingredients = { "嫩豆腐 1块", "猪肉末 100g", "豆瓣酱 1勺", "花椒粉 适量", "葱花 适量", "姜 适量", "蒜 适量", "生抽 1勺", "淀粉 适量" },
            Steps =
            {
                new CookingStep { Order = 1, Instruction = "豆腐切小块，放入盐水中浸泡5分钟备用", DurationSeconds = 300 },
                new CookingStep { Order = 2, Instruction = "热锅下油，放入豆瓣酱炒出红油", DurationSeconds = 60 },
                new CookingStep { Order = 3, Instruction = "下肉末炒散，加姜蒜末炒香", DurationSeconds = 120 },
                new CookingStep { Order = 4, Instruction = "加入适量水，下豆腐轻轻推散，煮3分钟", DurationSeconds = 180 },
                new CookingStep { Order = 5, Instruction = "水淀粉勾芡，让汤汁浓稠", DurationSeconds = 60 },
                new CookingStep { Order = 6, Instruction = "撒上花椒粉和葱花，出锅装盘", DurationSeconds = 30 }
            },
            Tips = { "豆腐焯水可去豆腥味", "勾芡后轻轻推动，避免豆腐碎", "花椒粉最后放，香味更浓郁" },
            CreatedAt = DateTime.Now
        });

        _builtInRecipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "清蒸鲈鱼",
            Description = "鲜嫩清香，保留食材本味的健康做法",
            CookTimeMinutes = 25,
            DifficultyLevel = 2,
            Ingredients = { "鲈鱼 1条(约500g)", "姜丝 适量", "葱丝 适量", "生抽 3勺", "料酒 1勺", "盐 少许", "食用油 2勺" },
            Steps =
            {
                new CookingStep { Order = 1, Instruction = "鲈鱼去鳞去内脏，两面划花刀，用盐和料酒腌制10分钟", DurationSeconds = 600 },
                new CookingStep { Order = 2, Instruction = "鱼身内外擦干，鱼腹和刀口塞入姜片", DurationSeconds = 120 },
                new CookingStep { Order = 3, Instruction = "蒸锅水开后，放入鱼大火蒸8分钟", DurationSeconds = 480 },
                new CookingStep { Order = 4, Instruction = "蒸好后倒掉蒸出的水，铺上葱丝姜丝", DurationSeconds = 60 },
                new CookingStep { Order = 5, Instruction = "淋上生抽，烧热油浇在鱼身上", DurationSeconds = 60 }
            },
            Tips = { "鱼一定要新鲜，活鱼最佳", "蒸的时间视鱼大小调整，每500g蒸8分钟", "浇热油时声音滋滋响才对" },
            CreatedAt = DateTime.Now
        });

        _builtInRecipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Name = "蒜蓉炒生菜",
            Description = "简单快手，清爽可口的家常蔬菜",
            CookTimeMinutes = 10,
            DifficultyLevel = 1,
            Ingredients = { "生菜 1棵", "大蒜 4瓣", "盐 适量", "食用油 适量", "生抽 少许" },
            Steps =
            {
                new CookingStep { Order = 1, Instruction = "生菜洗净，大片撕开，大蒜切末", DurationSeconds = 120 },
                new CookingStep { Order = 2, Instruction = "热锅大火烧油，油热后下蒜末爆香", DurationSeconds = 30 },
                new CookingStep { Order = 3, Instruction = "下生菜大火翻炒30秒，加盐调味", DurationSeconds = 60 },
                new CookingStep { Order = 4, Instruction = "沿锅边淋少许生抽，翻炒均匀出锅", DurationSeconds = 30 }
            },
            Tips = { "生菜不耐炒，大火快炒保持翠绿", "出锅前再加盐，避免出水过多", "喜欢蒜香浓郁可多放蒜末" },
            CreatedAt = DateTime.Now
        });
    }
}
