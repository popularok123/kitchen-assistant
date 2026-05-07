using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using KitchenAssistant.Core.Models;
using KitchenAssistant.Server.Data;

namespace KitchenAssistant.Server.Hubs;

public class CookingHub : Hub
{
    private static readonly ConcurrentDictionary<string, User> _connectedUsers = new();
    private static readonly ConcurrentQueue<ChatMessage> _messageHistory = new();
    private static readonly ConcurrentDictionary<Guid, CookingSession> _activeSessions = new();
    private static readonly ConcurrentDictionary<Guid, LiveRoom> _liveRooms = new();
    private static bool _historyLoaded = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private readonly PersistenceService _persistence;

    public CookingHub(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public override async Task OnConnectedAsync()
    {
        await EnsureHistoryLoadedAsync();
        await base.OnConnectedAsync();
    }

    private async Task EnsureHistoryLoadedAsync()
    {
        if (_historyLoaded) return;
        await _initLock.WaitAsync();
        try
        {
            if (_historyLoaded) return;
            var messages = await _persistence.LoadRecentMessagesAsync(100);
            foreach (var msg in messages)
                _messageHistory.Enqueue(msg);
            _historyLoaded = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task JoinRoom(string userName)
    {
        var user = new User
        {
            ConnectionId = Context.ConnectionId,
            Name = userName,
            IsOnline = true,
            JoinedAt = DateTime.Now
        };

        _connectedUsers[Context.ConnectionId] = user;

        await Clients.All.SendAsync("UserJoined", user);
        await Clients.Caller.SendAsync("MessageHistory", _messageHistory.TakeLast(50).ToList());
        await Clients.All.SendAsync("OnlineUsers", _connectedUsers.Values.OrderBy(u => u.JoinedAt).ToList());
        await Clients.Caller.SendAsync("ActiveSessions", _activeSessions.Values.ToList());
    }

    public async Task SendMessage(string content)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            return;

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserName = user.Name,
            Content = content,
            Timestamp = DateTime.Now,
            Type = MessageType.Normal
        };

        _messageHistory.Enqueue(message);
        TrimMessageHistory();
        await _persistence.SaveMessageAsync(message);
        await Clients.All.SendAsync("ReceiveMessage", message);
    }

    public async Task BroadcastCookingProgress(string recipeName, int currentStep, string stepDescription)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            return;

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserName = "系统",
            Content = $"🍳 {user.Name} 正在做「{recipeName}」：第{currentStep}步 - {stepDescription}",
            Timestamp = DateTime.Now,
            Type = MessageType.CookingProgress
        };

        _messageHistory.Enqueue(message);
        TrimMessageHistory();
        await Clients.All.SendAsync("ReceiveMessage", message);
    }

    public async Task RecallMessage(Guid messageId)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user)) return;

        var found = _messageHistory.FirstOrDefault(m => m.Id == messageId && m.UserName == user.Name);
        if (found == null) return;

        var recallable = (DateTime.Now - found.Timestamp).TotalMinutes <= 2;
        if (!recallable) return;

        var updated = _messageHistory.Where(m => m.Id != messageId).ToList();
        while (_messageHistory.TryDequeue(out _)) { }
        foreach (var m in updated) _messageHistory.Enqueue(m);

        await Clients.All.SendAsync("MessageRecalled", messageId);
    }

    public async Task SendPrivateMessage(string targetUserName, string content)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var sender)) return;

        var target = _connectedUsers.Values.FirstOrDefault(u =>
            u.Name.Equals(targetUserName, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            var notFound = new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserName = "系统",
                Content = $"用户 {targetUserName} 不在线",
                Timestamp = DateTime.Now,
                Type = MessageType.System
            };
            await Clients.Caller.SendAsync("ReceiveMessage", notFound);
            return;
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserName = sender.Name,
            Content = content,
            Timestamp = DateTime.Now,
            Type = MessageType.Private,
            TargetUserName = target.Name
        };

        await Clients.Client(target.ConnectionId).SendAsync("ReceiveMessage", message);
        await Clients.Caller.SendAsync("ReceiveMessage", message);
    }

    public async Task<Guid> CreateCookingSession(string recipeName, Guid recipeId)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            return Guid.Empty;

        var session = new CookingSession
        {
            Id = Guid.NewGuid(),
            Name = $"{user.Name} 的 {recipeName}",
            RecipeId = recipeId,
            RecipeName = recipeName,
            HostUserId = Context.ConnectionId,
            HostUserName = user.Name,
            Status = CookingSessionStatus.Preparing,
            StartTime = DateTime.Now
        };

        session.Participants.Add(new SessionParticipant
        {
            UserId = Context.ConnectionId,
            UserName = user.Name,
            JoinedAt = DateTime.Now,
            IsOnline = true
        });

        _activeSessions[session.Id] = session;
        await _persistence.SaveSessionAsync(session);

        await Clients.All.SendAsync("SessionCreated", session);

        var sysMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserName = "系统",
            Content = $"🎉 {user.Name} 创建了烹饪会话：{session.Name}",
            Timestamp = DateTime.Now,
            Type = MessageType.System
        };
        _messageHistory.Enqueue(sysMsg);
        TrimMessageHistory();
        await Clients.All.SendAsync("ReceiveMessage", sysMsg);

        return session.Id;
    }

    public async Task JoinCookingSession(Guid sessionId)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            return;

        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return;

        if (!session.Participants.Any(p => p.UserId == Context.ConnectionId))
        {
            session.Participants.Add(new SessionParticipant
            {
                UserId = Context.ConnectionId,
                UserName = user.Name,
                JoinedAt = DateTime.Now,
                IsOnline = true
            });

            await Clients.All.SendAsync("SessionUpdated", session);

            var sysMsg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserName = "系统",
                Content = $"👋 {user.Name} 加入了「{session.Name}」",
                Timestamp = DateTime.Now,
                Type = MessageType.System
            };
            _messageHistory.Enqueue(sysMsg);
            TrimMessageHistory();
            await Clients.All.SendAsync("ReceiveMessage", sysMsg);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
    }

    public async Task AssignTask(Guid sessionId, int stepIndex, string stepDescription)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            return;

        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return;

        var existingTask = session.TaskAssignments.FirstOrDefault(t => t.StepIndex == stepIndex);
        if (existingTask != null)
        {
            existingTask.AssignedUserId = Context.ConnectionId;
            existingTask.AssignedUserName = user.Name;
            existingTask.Status = Core.Models.TaskStatus.InProgress;
            existingTask.StartedAt = DateTime.Now;
        }
        else
        {
            session.TaskAssignments.Add(new TaskAssignment
            {
                Id = Guid.NewGuid(),
                StepIndex = stepIndex,
                StepDescription = stepDescription,
                AssignedUserId = Context.ConnectionId,
                AssignedUserName = user.Name,
                Status = Core.Models.TaskStatus.InProgress,
                StartedAt = DateTime.Now
            });
        }

        await Clients.Group(sessionId.ToString()).SendAsync("SessionUpdated", session);
    }

    public async Task CompleteTask(Guid sessionId, int stepIndex)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return;

        var task = session.TaskAssignments.FirstOrDefault(t => t.StepIndex == stepIndex);
        if (task != null)
        {
            task.Status = Core.Models.TaskStatus.Completed;
            task.CompletedAt = DateTime.Now;

            if (session.TaskAssignments.All(t => t.Status == Core.Models.TaskStatus.Completed))
            {
                session.Status = CookingSessionStatus.Completed;
                session.EndTime = DateTime.Now;
                await _persistence.SaveSessionAsync(session);
            }

            await Clients.Group(sessionId.ToString()).SendAsync("SessionUpdated", session);
        }
    }

    public async Task UpdateSessionStep(Guid sessionId, int currentStep)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
            return;

        session.CurrentStepIndex = currentStep;
        session.Status = CookingSessionStatus.Cooking;

        await Clients.Group(sessionId.ToString()).SendAsync("SessionUpdated", session);
    }

    // ── WebRTC 信令 ──────────────────────────────────────

    public async Task RequestStream(Guid roomId)
    {
        if (!_liveRooms.TryGetValue(roomId, out var room) || !room.IsLive) return;
        await Clients.Client(room.HostConnectionId).SendAsync("ViewerWantsStream", Context.ConnectionId);
    }

    public async Task SendOffer(string viewerConnectionId, string sdp)
    {
        await Clients.Client(viewerConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, sdp);
    }

    public async Task SendAnswer(string hostConnectionId, string sdp)
    {
        await Clients.Client(hostConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, sdp);
    }

    public async Task RelayIce(string targetConnectionId, string candidate)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveIce", Context.ConnectionId, candidate);
    }

    // ── 厨房直播间 ──────────────────────────────────────

    public async Task<Guid> CreateLiveRoom(string recipeName, Guid recipeId)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user)) return Guid.Empty;

        var room = new LiveRoom
        {
            Id = Guid.NewGuid(),
            Name = $"{user.Name} 的直播间",
            HostConnectionId = Context.ConnectionId,
            HostUserName = user.Name,
            RecipeId = recipeId,
            RecipeName = recipeName,
            CurrentStepIndex = 0,
            StartTime = DateTime.Now,
            IsLive = true
        };

        _liveRooms[room.Id] = room;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"live_{room.Id}");
        await Clients.All.SendAsync("LiveRoomCreated", room);
        await Clients.Caller.SendAsync("LiveRoomList", _liveRooms.Values.Where(r => r.IsLive).ToList());
        return room.Id;
    }

    public async Task JoinLiveRoom(Guid roomId)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user)) return;
        if (!_liveRooms.TryGetValue(roomId, out var room) || !room.IsLive) return;

        if (!room.Viewers.Any(v => v.ConnectionId == Context.ConnectionId))
        {
            room.Viewers.Add(new LiveViewer
            {
                ConnectionId = Context.ConnectionId,
                UserName = user.Name,
                JoinedAt = DateTime.Now
            });
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"live_{roomId}");
        await Clients.Group($"live_{roomId}").SendAsync("LiveRoomUpdated", room);
    }

    public async Task LeaveLiveRoom(Guid roomId)
    {
        if (!_liveRooms.TryGetValue(roomId, out var room)) return;

        room.Viewers.RemoveAll(v => v.ConnectionId == Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"live_{roomId}");

        if (room.Viewers.Count > 0)
            await Clients.Group($"live_{roomId}").SendAsync("LiveRoomUpdated", room);
    }

    public async Task UpdateLiveStep(Guid roomId, int stepIndex)
    {
        if (!_liveRooms.TryGetValue(roomId, out var room)) return;
        if (room.HostConnectionId != Context.ConnectionId) return;

        room.CurrentStepIndex = stepIndex;
        await Clients.Group($"live_{roomId}").SendAsync("LiveRoomUpdated", room);
    }

    public async Task SendDanmaku(Guid roomId, string content)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user)) return;
        if (!_liveRooms.ContainsKey(roomId)) return;

        var danmaku = new Danmaku
        {
            RoomId = roomId,
            UserName = user.Name,
            Content = content,
            Timestamp = DateTime.Now
        };

        await Clients.Group($"live_{roomId}").SendAsync("DanmakuReceived", danmaku);
    }

    public async Task AskQuestion(Guid roomId, string content)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var user)) return;
        if (!_liveRooms.TryGetValue(roomId, out var room)) return;

        var question = new LiveQuestion
        {
            Id = Guid.NewGuid(),
            AskUserName = user.Name,
            Content = content,
            AskedAt = DateTime.Now
        };

        room.Questions.Add(question);
        await Clients.Group($"live_{roomId}").SendAsync("QuestionAsked", question);
    }

    public async Task AnswerQuestion(Guid roomId, Guid questionId, string answer)
    {
        if (!_liveRooms.TryGetValue(roomId, out var room)) return;
        if (room.HostConnectionId != Context.ConnectionId) return;

        var question = room.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null) return;

        question.Answer = answer;
        question.IsAnswered = true;
        await Clients.Group($"live_{roomId}").SendAsync("QuestionAnswered", question);
    }

    public async Task EndLiveRoom(Guid roomId)
    {
        if (!_liveRooms.TryGetValue(roomId, out var room)) return;

        // 允许通过连接ID或用户名鉴权（刷新页面后连接ID会变）
        var isAuthorized = room.HostConnectionId == Context.ConnectionId;
        if (!isAuthorized && _connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            isAuthorized = room.HostUserName == user.Name;
        if (!isAuthorized) return;

        room.IsLive = false;
        _liveRooms.TryRemove(roomId, out _);
        await Clients.All.SendAsync("LiveRoomEnded", roomId);
    }

    public async Task GetLiveRooms()
    {
        await Clients.Caller.SendAsync("LiveRoomList", _liveRooms.Values.Where(r => r.IsLive).ToList());
    }

    private static void TrimMessageHistory()
    {
        while (_messageHistory.Count > 100)
            _messageHistory.TryDequeue(out _);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectedUsers.TryRemove(Context.ConnectionId, out var user))
        {
            await Clients.All.SendAsync("UserLeft", user);
            await Clients.All.SendAsync("OnlineUsers", _connectedUsers.Values.OrderBy(u => u.JoinedAt).ToList());
        }

        await base.OnDisconnectedAsync(exception);
    }
}
