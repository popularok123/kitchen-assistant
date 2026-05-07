# KitchenAssistant.Client

macOS 控制台应用，功能与 Web 端对等，额外支持语音播报和本地持久化自定义菜谱。

## 运行

```bash
dotnet run --project src/KitchenAssistant.Client
# 需要先启动 KitchenAssistant.Server（:5001）
# 服务不可达时自动降级为离线模式（聊天和协作不可用）
```

> **已知 Bug**：`ChatService` 默认连接 `localhost:5000`，但服务端运行在 `:5001`。
> 临时修复：将 `ChatService.cs` 构造函数中的 `serverUrl` 默认值改为 `:5001`。

## 目录结构

```
Program.cs                   # 入口 + 全部菜单逻辑（单文件）
Services/
  ChatService.cs             # SignalR 客户端（事件驱动，区别于 Web 版）
  MacTextToSpeechService.cs  # 调用 macOS `say` 命令实现 TTS
  TimerService.cs            # 多计时器，后台线程倒计时
  ShoppingListService.cs     # 购物清单（与 Web 版独立实现）
  RecipeStorageService.cs    # 自定义菜谱持久化到 JSON 文件
```

## 与 Web 版的关键差异

| 特性 | Web | 控制台 |
|------|-----|--------|
| 菜谱服务 | `Web.Services.RecipeService` | `Core.Services.RecipeService` |
| 自定义菜谱 | 无 | 支持增删，保存到 JSON |
| TTS 语音 | 无 | macOS `say` 命令 |
| 菜谱持久化 | 无（内存） | `RecipeStorageService` |
| ChatService | `OnUpdate` 统一事件 | 细粒度事件（MessageReceived/UserJoined/...） |
| 购物清单 | `Web.Services.ShoppingListService` | `Client.Services.ShoppingListService` |

## 服务说明

### ChatService（Client）
- 细粒度事件：`MessageReceived`、`UserJoined`、`UserLeft`、`OnlineUsersUpdated`、`SessionCreated`、`SessionUpdated`
- `CurrentSession` 属性跟踪已加入的会话
- 与 Web 版 `ChatService` 是**完全独立的实现**

### MacTextToSpeechService
- 调用系统命令 `say -v Ting-Ting "{text}"`（中文语音）
- 仅支持 macOS，Windows/Linux 需替换实现
- 实现 `ITextToSpeechService` 接口（在 Core 中定义）

### RecipeStorageService
- 将 `Core.Services.RecipeService` 中的自定义菜谱和收藏序列化为 JSON
- 每次修改后需手动调用 `Save()`（Program.cs 中）
- 存储路径为可执行文件同目录下的 `recipes.json`

### TimerService
- 支持同时运行多个计时器（后台线程）
- 计时结束触发 `TimerCompleted` 事件，可配置回调（调用 TTS 播报）
- 实现 `ITimerService` + `IDisposable`，退出时必须 Dispose

## 注意事项

- Program.cs 是单文件大类，所有菜单逻辑集中在此，扩展功能时注意保持菜单入口一致
- 控制台读写为阻塞式，不适合在计时器回调中直接操作 Console
