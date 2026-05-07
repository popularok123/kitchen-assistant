# KitchenAssistant — 厨房协作助手

家庭厨房协作工具，支持菜谱管理、多人实时协作烹饪、聊天室、购物清单。

## 解决方案结构

```
KitchenAssistant.sln
src/
  KitchenAssistant.Core/      # 共享领域模型和服务接口 (类库)
  KitchenAssistant.Server/    # SignalR 服务端，监听 http://localhost:5001
  KitchenAssistant.Web/       # Blazor WebAssembly 前端
  KitchenAssistant.Client/    # 控制台客户端 (macOS)
```

## 快速启动

```bash
# 先启动服务端（Web 和控制台客户端都依赖它）
dotnet run --project src/KitchenAssistant.Server

# 另一个终端启动 Web 前端
dotnet run --project src/KitchenAssistant.Web

# 或启动控制台客户端
dotnet run --project src/KitchenAssistant.Client
```

## 架构概览

```
[Web / Console Client]
       |
       | SignalR WebSocket
       v
  [KitchenAssistant.Server]  :5001/cookingHub
       |
       | 广播事件到所有客户端
       v
  [所有在线客户端同步状态]
```

- **服务端状态全在内存中（static）**，重启后丢失
- Web 和控制台客户端互通：同一 SignalR Hub，消息实时共享
- Core 类库无依赖，被其他三个项目引用

## 已知问题

- 控制台客户端 `ChatService` 默认连接 `localhost:5000`，但服务端运行在 `:5001`，需手动对齐
- 服务端 CORS 配置为 `AllowAll`，仅适用于开发环境

## 构建验证

```bash
dotnet build
```
