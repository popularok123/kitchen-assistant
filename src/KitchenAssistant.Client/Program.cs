using KitchenAssistant.Client.Services;
using KitchenAssistant.Core.Models;
using KitchenAssistant.Core.Services;

namespace KitchenAssistant.Client;

class Program
{
    private static RecipeService _recipeService = new();
    private static ITextToSpeechService _ttsService = new MacTextToSpeechService();
    private static ITimerService _timerService = new TimerService();
    private static IShoppingListService _shoppingListService = new ShoppingListService(_recipeService);
    private static RecipeStorageService _recipeStorageService = new(_recipeService);
    private static ChatService? _chatService;
    private static string? _userName;
    private static List<ChatMessage> _messages = [];

    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += (s, e) =>
        {
            _ttsService.Stop();
            e.Cancel = false;
        };

        ShowWelcomeScreen();

        Console.Write("请输入你的昵称: ");
        _userName = Console.ReadLine()?.Trim() ?? "匿名厨师";

        await ConnectToServer();

        await MainMenu();
    }

    static void ShowWelcomeScreen()
    {
        Console.Clear();
        Console.WriteLine("========================================");
        Console.WriteLine("      🍳 厨房协作助手 🍳            ");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("  功能：");
        Console.WriteLine("  • 查看菜谱和烹饪攻略");
        Console.WriteLine("  • 语音播报烹饪步骤");
        Console.WriteLine("  • 与其他成员实时交流");
        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        Console.WriteLine();
    }

    static async Task ConnectToServer()
    {
        Console.Write("正在连接服务器...");
        _chatService = new ChatService();

        try
        {
            await _chatService.ConnectAsync();
            await _chatService.JoinRoomAsync(_userName!);

            _chatService.MessageReceived += OnMessageReceived;
            _chatService.UserJoined += user =>
            {
                _messages.Add(new ChatMessage
                {
                    Content = $"🎉 {user.Name} 加入了厨房",
                    Type = MessageType.System,
                    Timestamp = DateTime.Now
                });
            };
            _chatService.UserLeft += user =>
            {
                _messages.Add(new ChatMessage
                {
                    Content = $"👋 {user.Name} 离开了厨房",
                    Type = MessageType.System,
                    Timestamp = DateTime.Now
                });
            };

            Console.WriteLine(" ✓ 已连接");
            await Task.Delay(800);
        }
        catch
        {
            Console.WriteLine(" ✗ 连接失败");
            Console.WriteLine("将以离线模式运行（聊天功能不可用）");
            await Task.Delay(1500);
        }
    }

    static void OnMessageReceived(ChatMessage msg)
    {
        _messages.Add(msg);
    }

    static async Task MainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine($"       欢迎, {_userName}!          ");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("  [1] 📖 查看菜谱列表");
            Console.WriteLine("  [2] 💬 进入聊天室");
            Console.WriteLine("  [3] ⏰ 计时器管理");
            Console.WriteLine("  [4] 🛒 购物清单");
            Console.WriteLine("  [5] 👥 协作烹饪");
            Console.WriteLine("  [6] 📝 菜谱管理");
            Console.WriteLine();

            var activeTimers = _timerService.GetActiveTimers();
            if (activeTimers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ! 当前有 {activeTimers.Count} 个计时器运行中");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.Write("请选择: ");

            var choice = Console.ReadKey();
            Console.WriteLine();

            switch (choice.Key)
            {
                case ConsoleKey.D1:
                    await ShowRecipeList();
                    break;
                case ConsoleKey.D2:
                    await ShowChatRoom();
                    break;
                case ConsoleKey.D3:
                    await ManageTimers();
                    break;
                case ConsoleKey.D4:
                    await ManageShoppingList();
                    break;
                case ConsoleKey.D5:
                    await ShowCollaborativeCooking();
                    break;
                case ConsoleKey.D6:
                    await ShowRecipeManagement();
                    break;
                case ConsoleKey.D0:
                    if (_chatService != null)
                        await _chatService.DisposeAsync();
                    if (_timerService is IDisposable disposable)
                        disposable.Dispose();
                    Console.WriteLine("再见！");
                    return;
            }
        }
    }

    static async Task ShowRecipeList()
    {
        var recipes = _recipeService.GetAllRecipes();
        string? currentFilter = null;

        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("          📖 菜谱列表                ");
            Console.WriteLine("========================================");
            if (!string.IsNullOrEmpty(currentFilter))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  当前筛选: {currentFilter}");
                Console.ResetColor();
            }
            Console.WriteLine();

            if (recipes.Count == 0)
            {
                Console.WriteLine("  没有找到匹配的菜谱");
                Console.WriteLine();
            }
            else
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    var recipe = recipes[i];
                    Console.WriteLine($"  [{i + 1}] {recipe.Name}");
                    Console.WriteLine($"      {recipe.Description}");
                    Console.WriteLine($"      ⏱️ {recipe.CookTimeMinutes}分钟  |  难度: {new string('⭐', recipe.DifficultyLevel)}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  [编号] 选择菜谱开始烹饪");
            Console.WriteLine("  [S] 搜索菜谱");
            Console.WriteLine("  [F] 筛选条件");
            Console.WriteLine("  [R] 重置筛选");
            Console.WriteLine("  [0] 返回主菜单");
            Console.WriteLine();
            Console.Write("请选择: ");

            var input = Console.ReadLine()?.Trim().ToUpper();

            if (input == "0") return;

            if (input == "S")
            {
                Console.Write("  输入搜索关键词: ");
                var keyword = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(keyword))
                {
                    recipes = _recipeService.SearchRecipes(keyword);
                    currentFilter = $"搜索: {keyword}";
                }
            }
            else if (input == "F")
            {
                recipes = await ShowFilterMenu();
                currentFilter = "自定义筛选";
            }
            else if (input == "R")
            {
                recipes = _recipeService.GetAllRecipes();
                currentFilter = null;
            }
            else if (int.TryParse(input, out var index))
            {
                if (index >= 1 && index <= recipes.Count)
                {
                    await StartCooking(recipes[index - 1]);
                }
            }
        }
    }

    static async Task ShowRecipeManagement()
    {
        while (true)
        {
            var recipes = _recipeService.GetAllRecipes();
            var favorites = _recipeService.GetFavoriteRecipes();
            var customRecipes = _recipeService.CustomRecipes;

            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        📝 菜谱管理                  ");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"  总菜谱数: {recipes.Count}");
            Console.WriteLine($"  收藏: {favorites.Count}");
            Console.WriteLine($"  自定义: {customRecipes.Count}");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  [1] 收藏列表");
            Console.WriteLine("  [2] 添加自定义菜谱");
            Console.WriteLine("  [3] 管理自定义菜谱");
            Console.WriteLine("  [0] 返回");
            Console.WriteLine();
            Console.Write("请选择: ");

            var input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "0") return;

            if (input == "1")
            {
                await ShowFavoriteRecipes();
            }
            else if (input == "2")
            {
                await AddCustomRecipe();
            }
            else if (input == "3")
            {
                await ManageCustomRecipes();
            }
        }
    }

    static async Task ShowFavoriteRecipes()
    {
        while (true)
        {
            var favorites = _recipeService.GetFavoriteRecipes();
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        ⭐ 我的收藏                  ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            if (favorites.Count == 0)
            {
                Console.WriteLine("  暂无收藏的菜谱");
                Console.WriteLine();
                Console.WriteLine("按任意键返回...");
                Console.ReadKey();
                return;
            }

            for (int i = 0; i < favorites.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {favorites[i].Name}");
            }

            Console.WriteLine();
            Console.WriteLine("  [编号] 开始烹饪  [0] 返回");
            Console.Write("请选择: ");

            if (int.TryParse(Console.ReadLine(), out var idx))
            {
                if (idx == 0) return;
                if (idx >= 1 && idx <= favorites.Count)
                {
                    await StartCooking(favorites[idx - 1]);
                }
            }
        }
    }

    static async Task AddCustomRecipe()
    {
        Console.Clear();
        Console.WriteLine("========================================");
        Console.WriteLine("        ➕ 添加新菜谱                ");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.Write("  菜谱名称: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        Console.Write("  简介: ");
        var description = Console.ReadLine()?.Trim() ?? "";

        Console.Write("  烹饪时间(分钟): ");
        if (!int.TryParse(Console.ReadLine(), out var cookTime)) cookTime = 30;

        Console.Write("  难度(1-3): ");
        if (!int.TryParse(Console.ReadLine(), out var difficulty)) difficulty = 2;

        Console.WriteLine();
        Console.WriteLine("  添加食材（每行一个，空行结束）:");
        Console.WriteLine("  格式: 食材名 数量");
        var ingredients = new List<string>();
        while (true)
        {
            Console.Write("  > ");
            var ing = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(ing)) break;
            ingredients.Add(ing);
        }

        Console.WriteLine();
        Console.WriteLine("  添加步骤（每行一个，空行结束）:");
        var steps = new List<CookingStep>();
        var stepOrder = 1;
        while (true)
        {
            Console.Write($"  步骤 {stepOrder}: ");
            var stepDesc = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(stepDesc)) break;

            Console.Write($"  预计秒数(可选): ");
            var secStr = Console.ReadLine()?.Trim();
            int.TryParse(secStr, out var sec);

            steps.Add(new CookingStep
            {
                Order = stepOrder,
                Instruction = stepDesc,
                DurationSeconds = sec
            });
            stepOrder++;
        }

        var recipe = new Recipe
        {
            Name = name,
            Description = description,
            CookTimeMinutes = cookTime,
            DifficultyLevel = Math.Clamp(difficulty, 1, 3),
            Ingredients = ingredients,
            Steps = steps
        };

        _recipeService.AddRecipe(recipe);
        _recipeStorageService.Save();

        Console.WriteLine();
        Console.WriteLine("  ✅ 菜谱已添加！");
        await Task.Delay(1000);
    }

    static async Task ManageCustomRecipes()
    {
        while (true)
        {
            var customRecipes = _recipeService.CustomRecipes.ToList();
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        📝 自定义菜谱管理            ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            if (customRecipes.Count == 0)
            {
                Console.WriteLine("  暂无自定义菜谱");
                Console.WriteLine();
                Console.WriteLine("按任意键返回...");
                Console.ReadKey();
                return;
            }

            for (int i = 0; i < customRecipes.Count; i++)
            {
                var r = customRecipes[i];
                var fav = _recipeService.IsFavorite(r.Id) ? "⭐ " : "";
                Console.WriteLine($"  [{i + 1}] {fav}{r.Name}");
            }

            Console.WriteLine();
            Console.WriteLine("  [编号] 切换收藏  [D+编号] 删除  [0] 返回");
            Console.Write("请选择: ");

            var input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "0") return;

            if (input?.StartsWith("D") == true && int.TryParse(input.Substring(1), out var delIdx))
            {
                if (delIdx >= 1 && delIdx <= customRecipes.Count)
                {
                    var recipeToDelete = customRecipes[delIdx - 1];
                    _recipeService.DeleteRecipe(recipeToDelete.Id);
                    _recipeStorageService.Save();
                    Console.WriteLine("已删除！");
                    await Task.Delay(500);
                }
            }
            else if (int.TryParse(input, out var idx) && idx >= 1 && idx <= customRecipes.Count)
            {
                var r = customRecipes[idx - 1];
                _recipeService.ToggleFavorite(r.Id);
                _recipeStorageService.Save();
            }
        }
    }

    static async Task<List<Recipe>> ShowFilterMenu()
    {
        var recipes = _recipeService.GetAllRecipes();

        Console.WriteLine();
        Console.WriteLine("  选择筛选条件:");
        Console.WriteLine("  [1] 按难度筛选");
        Console.WriteLine("  [2] 按烹饪时间筛选");
        Console.WriteLine("  [3] 按食材搜索");
        Console.Write("  请选择: ");

        var choice = Console.ReadKey().KeyChar.ToString();
        Console.WriteLine();

        if (choice == "1")
        {
            Console.Write("  最大难度(1-3): ");
            if (int.TryParse(Console.ReadLine(), out var diff) && diff >= 1 && diff <= 3)
            {
                recipes = _recipeService.FilterByDifficulty(null, diff);
            }
        }
        else if (choice == "2")
        {
            Console.Write("  最长时间(分钟): ");
            if (int.TryParse(Console.ReadLine(), out var time) && time > 0)
            {
                recipes = _recipeService.FilterByCookTime(time);
            }
        }
        else if (choice == "3")
        {
            Console.Write("  食材名称: ");
            var ingredient = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(ingredient))
            {
                recipes = _recipeService.FindRecipesByIngredient(ingredient);
            }
        }

        await Task.Delay(300);
        return recipes;
    }

    static async Task StartCooking(Recipe recipe)
    {
        int currentStep = 0;
        CancellationTokenSource? cts = null;

        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine($"      🍳 {recipe.Name}            ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            if (currentStep < recipe.Steps.Count)
            {
                var step = recipe.Steps[currentStep];
                Console.WriteLine($"  步骤 {currentStep + 1}/{recipe.Steps.Count}");
                Console.WriteLine();
                Console.WriteLine($"  {step.Instruction}");
                if (step.DurationSeconds > 0)
                    Console.WriteLine($"  预计时间: {step.DurationSeconds}秒");
                Console.WriteLine();

                if (_chatService?.IsConnected == true)
                {
                    await _chatService.BroadcastCookingProgressAsync(recipe.Name, currentStep + 1, step.Instruction);
                }

                ShowActiveTimers();

                Console.WriteLine("----------------------------------------");
                var isFav = _recipeService.IsFavorite(recipe.Id);
                Console.WriteLine($"  [F] {(isFav ? "取消收藏" : "加入收藏")}");
                Console.WriteLine("  [空格] 语音播报此步骤");
                Console.WriteLine("  [S] 开始此步骤计时");
                Console.WriteLine("  [M] 管理所有计时器");
                Console.WriteLine("  [→] 下一步");
                Console.WriteLine("  [←] 上一步");
                Console.WriteLine("  [T] 显示小贴士");
                Console.WriteLine("  [ESC] 退出烹饪");
                Console.WriteLine();

                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Spacebar:
                        cts?.Cancel();
                        cts = new CancellationTokenSource();
                        _ = _ttsService.SpeakAsync(step.Instruction, cts.Token);
                        Console.WriteLine("\n  正在播报...");
                        await Task.Delay(1000);
                        break;
                    case ConsoleKey.S:
                        if (step.DurationSeconds > 0)
                        {
                            var timerId = _timerService.StartTimer(
                                $"步骤{currentStep + 1}: {step.Instruction.Substring(0, Math.Min(15, step.Instruction.Length))}...",
                                step.DurationSeconds,
                                async (id, name) =>
                                {
                                    await _ttsService.SpeakAsync($"时间到！{name}");
                                });
                            Console.WriteLine($"\n  已开始计时: {step.DurationSeconds}秒");
                            await Task.Delay(1000);
                        }
                        else
                        {
                            Console.Write("\n  请输入计时秒数: ");
                            if (int.TryParse(Console.ReadLine(), out var sec) && sec > 0)
                            {
                                _timerService.StartTimer($"步骤{currentStep + 1}: 自定义计时", sec);
                                Console.WriteLine($"  已开始计时: {sec}秒");
                                await Task.Delay(800);
                            }
                        }
                        break;
                    case ConsoleKey.M:
                        await ManageTimers();
                        break;
                    case ConsoleKey.F:
                        _recipeService.ToggleFavorite(recipe.Id);
                        _recipeStorageService.Save();
                        var isFavorite = _recipeService.IsFavorite(recipe.Id);
                        Console.WriteLine(isFavorite ? "\n  已加入收藏！" : "\n  已取消收藏！");
                        await Task.Delay(800);
                        break;
                    case ConsoleKey.RightArrow:
                        currentStep++;
                        break;
                    case ConsoleKey.LeftArrow:
                        if (currentStep > 0) currentStep--;
                        break;
                    case ConsoleKey.T:
                        ShowTips(recipe);
                        break;
                    case ConsoleKey.Escape:
                        _ttsService.Stop();
                        return;
                }
            }
            else
            {
                Console.WriteLine("  🎉 恭喜！烹饪完成！");
                Console.WriteLine();
                Console.WriteLine("  按任意键返回...");
                Console.ReadKey();
                return;
            }
        }
    }

    static void ShowTips(Recipe recipe)
    {
        Console.WriteLine();
        Console.WriteLine("  💡 小贴士:");
        foreach (var tip in recipe.Tips)
        {
            Console.WriteLine($"   • {tip}");
        }
        Console.WriteLine();
        Console.WriteLine("  按任意键继续...");
        Console.ReadKey();
    }

    static void ShowActiveTimers()
    {
        var timers = _timerService.GetActiveTimers();
        if (timers.Count == 0)
            return;

        Console.WriteLine("  ⏰ 运行中的计时器:");
        foreach (var timer in timers)
        {
            var time = TimeSpan.FromSeconds(timer.RemainingSeconds);
            var status = timer.IsRunning ? "▶" : "⏸";
            Console.WriteLine($"    {status} [{timer.Id.ToString().Substring(0, 8)}] {timer.Name} - {time:mm\\:ss}");
        }
        Console.WriteLine();
    }

    static async Task ManageTimers()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        ⏰ 计时器管理                  ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var timers = _timerService.GetActiveTimers();
            if (timers.Count == 0)
            {
                Console.WriteLine("  暂无运行中的计时器");
                Console.WriteLine();
                Console.WriteLine("  [1] 新建计时器");
                Console.WriteLine("  [0] 返回");
                Console.WriteLine();
                Console.Write("请选择: ");

                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.D0) return;
                if (key.Key == ConsoleKey.D1)
                {
                    Console.Write("\n  计时器名称: ");
                    var name = Console.ReadLine() ?? "自定义计时";
                    Console.Write("  计时秒数: ");
                    if (int.TryParse(Console.ReadLine(), out var sec) && sec > 0)
                    {
                        _timerService.StartTimer(name, sec);
                        Console.WriteLine("  已开始计时！");
                        await Task.Delay(800);
                    }
                }
            }
            else
            {
                for (int i = 0; i < timers.Count; i++)
                {
                    var timer = timers[i];
                    var time = TimeSpan.FromSeconds(timer.RemainingSeconds);
                    var status = timer.IsRunning ? "运行中" : "已暂停";
                    Console.WriteLine($"  [{i + 1}] {timer.Name}");
                    Console.WriteLine($"      剩余: {time:mm\\:ss} | {status}");
                    Console.WriteLine();
                }
                Console.WriteLine("  [编号] 暂停/继续");
                Console.WriteLine("  [D+编号] 删除计时器");
                Console.WriteLine("  [N] 新建计时器");
                Console.WriteLine("  [0] 返回");
                Console.WriteLine();
                Console.Write("请选择: ");

                var input = Console.ReadLine()?.Trim().ToUpper();
                if (input == "0") return;
                if (input == "N")
                {
                    Console.Write("  计时器名称: ");
                    var name = Console.ReadLine() ?? "自定义计时";
                    Console.Write("  计时秒数: ");
                    if (int.TryParse(Console.ReadLine(), out var sec) && sec > 0)
                    {
                        _timerService.StartTimer(name, sec);
                        Console.WriteLine("  已开始计时！");
                        await Task.Delay(800);
                    }
                }
                else if (input?.StartsWith("D") == true && int.TryParse(input.Substring(1), out var delIdx))
                {
                    if (delIdx >= 1 && delIdx <= timers.Count)
                    {
                        _timerService.StopTimer(timers[delIdx - 1].Id);
                        Console.WriteLine("  已删除计时器");
                        await Task.Delay(500);
                    }
                }
                else if (int.TryParse(input, out var idx))
                {
                    if (idx >= 1 && idx <= timers.Count)
                    {
                        var timer = timers[idx - 1];
                        if (timer.IsRunning)
                            _timerService.PauseTimer(timer.Id);
                        else
                            _timerService.ResumeTimer(timer.Id);
                    }
                }
            }
        }
    }

    static async Task ManageShoppingList()
    {
        while (true)
        {
            var list = _shoppingListService.GetCurrentList();
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine($"        🛒 {list.Name}            ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var items = list.Items;
            if (items.Count == 0)
            {
                Console.WriteLine("  清单为空");
                Console.WriteLine();
            }
            else
            {
                var grouped = items.GroupBy(i => i.Category);
                foreach (var group in grouped)
                {
                    Console.WriteLine($"  [{group.Key}]");
                    var index = 1;
                    foreach (var item in group)
                    {
                        var check = item.IsPurchased ? "✓" : " ";
                        var display = $"{index}. [{check}] {item.Name}";
                        if (!string.IsNullOrEmpty(item.Quantity))
                            display += $" ({item.Quantity})";

                        if (item.IsPurchased)
                            Console.ForegroundColor = ConsoleColor.Gray;

                        Console.WriteLine($"    {display}");
                        Console.ResetColor();
                        index++;
                    }
                    Console.WriteLine();
                }
            }

            var purchasedCount = items.Count(i => i.IsPurchased);
            if (items.Count > 0)
            {
                Console.WriteLine($"  进度: {purchasedCount}/{items.Count} 已购买");
                Console.WriteLine();
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  [编号] 切换购买状态");
            Console.WriteLine("  [A] 添加物品");
            Console.WriteLine("  [R] 从菜谱生成");
            Console.WriteLine("  [C] 清空已购买");
            Console.WriteLine("  [D] 清空全部");
            Console.WriteLine("  [0] 返回");
            Console.WriteLine();
            Console.Write("请选择: ");

            var input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "0") return;

            if (input == "A")
            {
                Console.Write("  物品名称: ");
                var name = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    Console.Write("  数量(可选): ");
                    var qty = Console.ReadLine()?.Trim() ?? "";
                    _shoppingListService.AddItem(name, qty);
                }
            }
            else if (input == "R")
            {
                await GenerateShoppingListFromRecipe();
            }
            else if (input == "C")
            {
                _shoppingListService.ClearCompleted();
            }
            else if (input == "D")
            {
                _shoppingListService.ClearAll();
            }
            else if (int.TryParse(input, out var idx))
            {
                var allItems = list.Items.ToList();
                if (idx >= 1 && idx <= allItems.Count)
                {
                    _shoppingListService.ToggleItem(allItems[idx - 1].Id);
                }
            }
        }
    }

    static async Task GenerateShoppingListFromRecipe()
    {
        Console.WriteLine();
        Console.WriteLine("  选择菜谱:");
        var recipes = _recipeService.GetAllRecipes();
        for (int i = 0; i < recipes.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {recipes[i].Name}");
        }
        Console.Write("  请选择: ");
        if (int.TryParse(Console.ReadLine(), out var idx) && idx >= 1 && idx <= recipes.Count)
        {
            _shoppingListService.GenerateFromRecipe(recipes[idx - 1].Id);
            Console.WriteLine("  已生成购物清单！");
            await Task.Delay(800);
        }
    }

    static async Task ShowCollaborativeCooking()
    {
        if (_chatService == null || !_chatService.IsConnected)
        {
            Console.Clear();
            Console.WriteLine("未连接到服务器，协作功能不可用");
            Console.WriteLine("按任意键返回...");
            Console.ReadKey();
            return;
        }

        Guid? currentSessionId = null;

        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        👥 协作烹饪                  ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var sessions = _chatService.ActiveSessions
                .Where(s => s.Status != CookingSessionStatus.Completed)
                .ToList();

            if (sessions.Count == 0)
            {
                Console.WriteLine("  当前没有进行中的烹饪会话");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("  进行中的会话:");
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    var statusIcon = session.Status switch
                    {
                        CookingSessionStatus.Preparing => "⏳",
                        CookingSessionStatus.Cooking => "🔥",
                        CookingSessionStatus.Paused => "⏸️",
                        _ => ""
                    };
                    Console.WriteLine($"  [{i + 1}] {statusIcon} {session.Name}");
                    Console.WriteLine($"      主持人: {session.HostUserName}");
                    Console.WriteLine($"      参与人数: {session.Participants.Count}");
                    Console.WriteLine($"      当前步骤: {session.CurrentStepIndex + 1}");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  [编号] 加入会话");
            Console.WriteLine("  [C] 创建新会话");
            Console.WriteLine("  [0] 返回");
            Console.WriteLine();
            Console.Write("请选择: ");

            var input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "0") return;

            if (input == "C")
            {
                await CreateNewCookingSession();
            }
            else if (int.TryParse(input, out var idx) && idx >= 1 && idx <= sessions.Count)
            {
                currentSessionId = sessions[idx - 1].Id;
                await _chatService.JoinCookingSessionAsync(currentSessionId.Value);
                await ShowSessionDetails(currentSessionId.Value);
            }
        }
    }

    static async Task CreateNewCookingSession()
    {
        Console.WriteLine();
        Console.WriteLine("  选择菜谱创建会话:");
        var recipes = _recipeService.GetAllRecipes();
        for (int i = 0; i < recipes.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {recipes[i].Name}");
        }
        Console.Write("  请选择: ");

        if (int.TryParse(Console.ReadLine(), out var idx) && idx >= 1 && idx <= recipes.Count)
        {
            var recipe = recipes[idx - 1];
            var sessionId = await _chatService!.CreateCookingSessionAsync(recipe.Name, recipe.Id);
            await _chatService.JoinCookingSessionAsync(sessionId);
            Console.WriteLine("  会话已创建！");
            await Task.Delay(800);
            await ShowSessionDetails(sessionId, recipe);
        }
    }

    static async Task ShowSessionDetails(Guid sessionId, Recipe? recipe = null)
    {
        recipe ??= _recipeService.GetRecipeById(_chatService!.CurrentSession!.RecipeId);
        if (recipe == null) return;

        while (true)
        {
            var session = _chatService!.CurrentSession;
            if (session == null) break;

            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine($"        👥 {session.Name}");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"  菜谱: {session.RecipeName}");
            Console.WriteLine($"  参与人数: {session.Participants.Count}");
            Console.WriteLine($"  当前步骤: {session.CurrentStepIndex + 1}/{recipe.Steps.Count}");
            Console.WriteLine();

            Console.WriteLine("  参与者:");
            foreach (var p in session.Participants)
            {
                Console.WriteLine($"    • {p.UserName}");
            }
            Console.WriteLine();

            Console.WriteLine("  任务状态:");
            for (int i = 0; i < recipe.Steps.Count; i++)
            {
                var step = recipe.Steps[i];
                var task = session.TaskAssignments.FirstOrDefault(t => t.StepIndex == i);
                string status;
                if (task == null) status = "  [ ] 未分配";
                else if (task.Status == Core.Models.TaskStatus.InProgress) status = "  [🔥] 进行中";
                else status = "  [✓] 已完成";

                Console.WriteLine($"{status} 步骤{i + 1}: {step.Instruction.Substring(0, Math.Min(20, step.Instruction.Length))}...");
                if (task != null)
                {
                    Console.WriteLine($"       负责人: {task.AssignedUserName}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  [A] 认领当前步骤任务");
            Console.WriteLine("  [F] 完成当前步骤");
            Console.WriteLine("  [→] 下一步");
            Console.WriteLine("  [←] 上一步");
            Console.WriteLine("  [0] 退出会话");
            Console.WriteLine();
            Console.Write("请选择: ");

            var input = Console.ReadLine()?.Trim().ToUpper();
            if (input == "0") break;

            if (input == "A")
            {
                var step = recipe.Steps[session.CurrentStepIndex];
                await _chatService.AssignTaskAsync(sessionId, session.CurrentStepIndex, step.Instruction);
                Console.WriteLine("  已认领任务！");
                await Task.Delay(500);
            }
            else if (input == "F")
            {
                await _chatService.CompleteTaskAsync(sessionId, session.CurrentStepIndex);
                Console.WriteLine("  任务已完成！");
                await Task.Delay(500);
            }
            else if (input == "→" || input == ">")
            {
                if (session.CurrentStepIndex < recipe.Steps.Count - 1)
                {
                    await _chatService.UpdateSessionStepAsync(sessionId, session.CurrentStepIndex + 1);
                }
            }
            else if (input == "←" || input == "<")
            {
                if (session.CurrentStepIndex > 0)
                {
                    await _chatService.UpdateSessionStepAsync(sessionId, session.CurrentStepIndex - 1);
                }
            }
        }
    }

    static async Task ShowChatRoom()
    {
        if (_chatService == null || !_chatService.IsConnected)
        {
            Console.Clear();
            Console.WriteLine("未连接到服务器，聊天功能不可用");
            Console.WriteLine("按任意键返回...");
            Console.ReadKey();
            return;
        }

        var quickReplies = new[] { "👍 好的", "🔥 加油", "😋 好香", "🙏 帮忙", "✅ 完成" };

        while (true)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("          💬 厨房聊天室                ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var onlineUsers = _chatService.OnlineUsers;
            if (onlineUsers.Count > 0)
            {
                Console.Write($"  在线 ({onlineUsers.Count}): ");
                foreach (var user in onlineUsers)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{user.Name}  ");
                    Console.ResetColor();
                }
                Console.WriteLine();
                Console.WriteLine();
            }

            var displayMessages = _messages.TakeLast(12).ToList();
            foreach (var msg in displayMessages)
            {
                var timeStr = msg.Timestamp.ToString("HH:mm");
                switch (msg.Type)
                {
                    case MessageType.System:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"  [{timeStr}] {msg.Content}");
                        Console.ResetColor();
                        break;
                    case MessageType.CookingProgress:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  [{timeStr}] {msg.Content}");
                        Console.ResetColor();
                        break;
                    default:
                        Console.Write($"  [{timeStr}] ");
                        var content = msg.Content;
                        if (content.Contains("@"))
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.Write($"{msg.UserName}: ");
                            Console.ResetColor();
                            var parts = content.Split(' ');
                            foreach (var part in parts)
                            {
                                if (part.StartsWith("@"))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write($"{part} ");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    Console.Write($"{part} ");
                                }
                            }
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"{msg.UserName}: ");
                            Console.ResetColor();
                            Console.WriteLine($"{msg.Content}");
                        }
                        break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
            Console.WriteLine("  快捷回复:");
            for (int i = 0; i < quickReplies.Length; i++)
            {
                Console.Write($"  [{i + 1}] {quickReplies[i]}");
                if (i < quickReplies.Length - 1) Console.Write("  ");
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("  输入 @ 可提及用户，或输入快捷回复编号");
            Console.WriteLine("  空行退出");
            Console.Write("> ");

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                break;

            if (int.TryParse(input, out var idx) && idx >= 1 && idx <= quickReplies.Length)
            {
                await _chatService.SendMessageAsync(quickReplies[idx - 1]);
            }
            else
            {
                await _chatService.SendMessageAsync(input);
            }
        }
    }
}
