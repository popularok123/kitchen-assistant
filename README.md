# 🍳 厨房协作助手

一个简单的厨房做菜软件，支持菜谱展示、语音播报和成员实时交流。

## 功能特性

- 📖 **菜谱管理**：内置番茄炒蛋、红烧肉等经典菜谱
- 🗣️ **语音播报**：自动朗读烹饪步骤（Mac 支持）
- 💬 **实时聊天**：多人在线交流，共享烹饪进度
- 📊 **进度广播**：做菜进度自动同步给所有在线成员

## 项目结构

```
src/
├── KitchenAssistant.Core/      # 核心库
│   ├── Models/                # 数据模型
│   └── Services/              # 服务接口
├── KitchenAssistant.Server/   # SignalR 服务端
│   └── Hubs/                  # 聊天集线器
└── KitchenAssistant.Client/   # 控制台客户端
    └── Services/              # 客户端服务
```

## 运行方式

### 1. 启动服务端

```bash
cd src/KitchenAssistant.Server
dotnet run
```

服务将在 `http://localhost:5000` 启动。

### 2. 启动客户端（可多开）

```bash
cd src/KitchenAssistant.Client
dotnet run
```

## 使用说明

1. 输入昵称进入系统
2. 选择菜谱查看详细步骤
3. 按空格语音播报当前步骤
4. 进入聊天室与其他成员交流

## 技术栈

- .NET 8
- SignalR（实时通讯）
- macOS say 命令（语音合成）
