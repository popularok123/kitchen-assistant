# KitchenAssistant.Web

Blazor WebAssembly 前端，运行在浏览器中，通过 SignalR 连接服务端。

## 运行

```bash
dotnet run --project src/KitchenAssistant.Web
# 需要先启动 KitchenAssistant.Server（:5001）
```

## 页面路由

| 路径 | 文件 | 功能 |
|------|------|------|
| `/` | Home.razor | 登录/用户选择入口 |
| `/recipes` | Recipes.razor | 菜谱列表，支持搜索、难度筛选、收藏 |
| `/recipe/{id:guid}` | RecipeDetail.razor | 菜谱详情，含逐步烹饪模式和计时器 |
| `/chat` | Chat.razor | 实时聊天室 |
| `/collaborate` | Collaborate.razor | 多人协作烹饪，任务认领与进度跟踪 |
| `/shopping` | ShoppingList.razor | 购物清单，可从菜谱一键生成 |

## 服务注册（Program.cs）

| 服务 | 生命周期 | 说明 |
|------|----------|------|
| `RecipeService` | Singleton | 内存菜谱库，单例保持收藏状态 |
| `AppState` | Scoped | 用户登录状态，持久化到 localStorage |
| `ChatService` | Scoped | SignalR 客户端连接封装 |
| `ShoppingListService` | Scoped | 购物清单，依赖 RecipeService |
| `LocalStorageService` | Scoped | JS Interop 封装的 localStorage 操作 |

## 关键服务说明

### AppState
- `UserName` / `Avatar` 写入时自动同步到 localStorage
- `IsLoggedIn = !string.IsNullOrWhiteSpace(UserName)`
- 页面初始化时调用 `LoadFromStorageAsync()` 恢复登录态
- `DefaultAvatars` 为静态 emoji 数组，选择头像只改 emoji

### ChatService（Web）
- 连接 `http://localhost:5001/cookingHub`
- `ConnectAsync(userName)` 同时调用 Hub `JoinRoom`
- 消息列表上限 50 条（Hub 端限制，客户端不做额外截断）
- `OnUpdate` 事件用于通知 Razor 组件刷新

### Web.Services.RecipeService
- 与 `Core.Services.RecipeService` 是**两个独立类**
- 无自定义菜谱增删（Web 版仅内置菜谱）
- `OnFavoritesChanged` 事件供页面订阅，实现收藏按钮实时刷新

## Razor 页面模式

### 事件订阅释放
所有订阅 Service 事件的页面必须实现 `IDisposable`（或 `IAsyncDisposable`）并在 `Dispose` 中取消订阅：

```csharp
protected override void OnInitialized()
{
    SomeService.OnChange += StateHasChanged;
}
public void Dispose()
{
    SomeService.OnChange -= StateHasChanged;
}
```

### 聊天消息自动滚动
[Chat.razor](Pages/Chat.razor) 使用 `IJSRuntime` 调用 `window.scrollToBottom(element)`，该函数定义在 [wwwroot/index.html](wwwroot/index.html)。

## 静态资源

- `wwwroot/css/app.css`：全局样式，Bootstrap 为基础框架
- `wwwroot/index.html`：WASM 宿主页，包含 `scrollToBottom` JS 辅助函数
- 无自定义 JavaScript 模块，JS 交互通过 `IJSRuntime.InvokeVoidAsync` 内联调用
