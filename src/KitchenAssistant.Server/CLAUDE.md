# KitchenAssistant.Server

ASP.NET Core 最简 API + SignalR Hub，为 Web 和控制台客户端提供实时协作能力。

## 运行

```bash
dotnet run --project src/KitchenAssistant.Server
# 监听 http://localhost:5001
# Hub 端点: http://localhost:5001/cookingHub
```

`GET /` 返回健康检查文本，可用于验证服务是否启动。

## CookingHub 方法

### 客户端调用服务端（Hub Methods）

| 方法 | 参数 | 说明 |
|------|------|------|
| `JoinRoom` | userName | 加入聊天室，服务端记录连接 |
| `SendMessage` | content | 广播聊天消息 |
| `BroadcastCookingProgress` | recipeName, currentStep, stepDescription | 广播烹饪进度为系统消息 |
| `CreateCookingSession` | recipeName, recipeId | 创建协作会话，返回 `Guid` |
| `JoinCookingSession` | sessionId | 加入会话 SignalR Group |
| `AssignTask` | sessionId, stepIndex, stepDescription | 认领步骤任务 |
| `CompleteTask` | sessionId, stepIndex | 完成任务，全部完成时 Session 变 Completed |
| `UpdateSessionStep` | sessionId, currentStep | 更新当前步骤索引 |

### 服务端推送客户端（Client Events）

| 事件 | 数据 | 触发时机 |
|------|------|----------|
| `ReceiveMessage` | ChatMessage | 任何消息发送 |
| `UserJoined` | User | 新用户加入 |
| `UserLeft` | User | 用户断开 |
| `OnlineUsers` | List\<User\> | 用户列表变化 |
| `MessageHistory` | List\<ChatMessage\> | 仅发给新加入的 Caller |
| `ActiveSessions` | List\<CookingSession\> | 仅发给新加入的 Caller |
| `SessionCreated` | CookingSession | 广播给所有人 |
| `SessionUpdated` | CookingSession | 广播给 Session Group |

## 状态管理

所有状态存储在三个 **static 字段**中（进程内存，重启清空）：

```csharp
static Dictionary<string, User> _connectedUsers   // key = ConnectionId
static List<ChatMessage> _messageHistory           // 最多 50 条（取 TakeLast(50)）
static Dictionary<Guid, CookingSession> _activeSessions
```

## 重要约束

- **无持久化**：服务端重启所有会话、消息、用户状态清空
- **无认证**：用于开发/家庭局域网场景
- CORS 配置为 `AllowAll`，生产环境需收紧
- Session 内消息通过 SignalR Group（以 sessionId 字符串为组名）隔离广播
