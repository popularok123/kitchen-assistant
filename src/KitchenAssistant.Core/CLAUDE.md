# KitchenAssistant.Core

被所有其他项目引用的共享类库，包含领域模型和服务接口定义，无第三方依赖。

## 目录结构

```
Models/
  Recipe.cs          # Recipe, CookingStep
  CookingSession.cs  # CookingSession, SessionParticipant, TaskAssignment, 枚举
  ChatMessage.cs     # ChatMessage, MessageType 枚举
  ShoppingList.cs    # ShoppingList, ShoppingItem
  User.cs            # User (SignalR 连接用户)

Services/
  RecipeService.cs       # 菜谱 CRUD、收藏、搜索、筛选
  ITextToSpeechService.cs
  ITimerService.cs       # CookingTimer, TimerEventArgs 也在这里
  IShoppingListService.cs
```

## 关键模型

### Recipe
- `Ingredients`: `List<string>`，格式为 `"食材名 数量"`（如 `"鸡蛋 3个"`）
- `Steps`: `List<CookingStep>`，每步有 `DurationSeconds`（0 表示无计时）
- `DifficultyLevel`: 1=简单 / 2=中等 / 3=较难

### CookingSession
- `HostUserId` 存 SignalR `ConnectionId`（字符串，非用户名）
- `TaskAssignment.Status` 使用 `KitchenAssistant.Core.Models.TaskStatus` 枚举（避免与系统 `TaskStatus` 冲突，引用时需写全限定名）

### ChatMessage
- `Type = MessageType.System`：系统通知（加入/离开/创建会话）
- `Type = MessageType.CookingProgress`：烹饪进度广播
- `Type = MessageType.Normal`：普通聊天

## Core.Services.RecipeService

该类是**控制台客户端**使用的 RecipeService（有自定义菜谱增删、收藏持久化支持），与 Web 项目的 `KitchenAssistant.Web.Services.RecipeService` 是**两个不同的类**。

- `BuiltInRecipes`：内置菜谱（只读）
- `CustomRecipes`：用户添加的菜谱（可增删）
- 额外筛选方法：`FilterByDifficulty`、`FilterByCookTime`、`FindRecipesByIngredient`

## 注意事项

- 不允许在 Core 中引入任何框架依赖（保持纯 POCO）
- `TaskStatus` 枚举定义在此项目，在 Razor 页面和 Hub 中引用时写全限定名 `KitchenAssistant.Core.Models.TaskStatus`
