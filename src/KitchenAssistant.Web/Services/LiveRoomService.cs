using Microsoft.AspNetCore.SignalR.Client;
using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Web.Services;

public class LiveRoomService : IAsyncDisposable
{
    private readonly string _hubUrl;
    private HubConnection? _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;
    public List<LiveRoom> LiveRooms { get; } = [];
    public LiveRoom? CurrentRoom { get; private set; }
    public List<Danmaku> Danmakus { get; } = [];
    public List<LiveQuestion> Questions { get; } = [];

    public event Action? OnUpdate;
    // WebRTC 信令事件
    public event Action<string>? ViewerWantsStream;
    public event Action<string, string>? OfferReceived;
    public event Action<string, string>? AnswerReceived;
    public event Action<string, string>? IceReceived;

    public LiveRoomService(string hubUrl = "http://localhost:5001/cookingHub")
    {
        _hubUrl = hubUrl;
    }

    public async Task ConnectAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (_connection != null) await _connection.DisposeAsync();

        _connection = new HubConnectionBuilder().WithUrl(_hubUrl).Build();

        _connection.On<LiveRoom>("LiveRoomCreated", room =>
        {
            if (!LiveRooms.Any(r => r.Id == room.Id))
                LiveRooms.Add(room);
            OnUpdate?.Invoke();
        });

        _connection.On<List<LiveRoom>>("LiveRoomList", rooms =>
        {
            LiveRooms.Clear();
            LiveRooms.AddRange(rooms);
            OnUpdate?.Invoke();
        });

        _connection.On<LiveRoom>("LiveRoomUpdated", room =>
        {
            var idx = LiveRooms.FindIndex(r => r.Id == room.Id);
            if (idx >= 0) LiveRooms[idx] = room;
            else LiveRooms.Add(room);

            if (CurrentRoom?.Id == room.Id)
                CurrentRoom = room;

            OnUpdate?.Invoke();
        });

        _connection.On<Guid>("LiveRoomEnded", roomId =>
        {
            LiveRooms.RemoveAll(r => r.Id == roomId);
            if (CurrentRoom?.Id == roomId) CurrentRoom = null;
            OnUpdate?.Invoke();
        });

        _connection.On<Danmaku>("DanmakuReceived", danmaku =>
        {
            Danmakus.Add(danmaku);
            if (Danmakus.Count > 100) Danmakus.RemoveAt(0);
            OnUpdate?.Invoke();
        });

        _connection.On<LiveQuestion>("QuestionAsked", q =>
        {
            Questions.Add(q);
            OnUpdate?.Invoke();
        });

        _connection.On<LiveQuestion>("QuestionAnswered", answered =>
        {
            var idx = Questions.FindIndex(q => q.Id == answered.Id);
            if (idx >= 0) Questions[idx] = answered;
            OnUpdate?.Invoke();
        });

        _connection.On<string>("ViewerWantsStream", id => ViewerWantsStream?.Invoke(id));
        _connection.On<string, string>("ReceiveOffer", (from, sdp) => OfferReceived?.Invoke(from, sdp));
        _connection.On<string, string>("ReceiveAnswer", (from, sdp) => AnswerReceived?.Invoke(from, sdp));
        _connection.On<string, string>("ReceiveIce", (from, c) => IceReceived?.Invoke(from, c));

        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync("JoinRoom", userName, cancellationToken);
        await _connection.InvokeAsync("GetLiveRooms", cancellationToken);
    }

    public async Task<Guid> CreateRoomAsync(string recipeName, Guid recipeId, CancellationToken ct = default)
    {
        if (_connection == null) return Guid.Empty;
        var id = await _connection.InvokeAsync<Guid>("CreateLiveRoom", recipeName, recipeId, ct);
        CurrentRoom = LiveRooms.FirstOrDefault(r => r.Id == id);
        return id;
    }

    public async Task JoinRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("JoinLiveRoom", roomId, ct);
        CurrentRoom = LiveRooms.FirstOrDefault(r => r.Id == roomId);
        Danmakus.Clear();
        Questions.Clear();
        if (CurrentRoom != null)
            Questions.AddRange(CurrentRoom.Questions);
    }

    public async Task LeaveRoomAsync(CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        await _connection.InvokeAsync("LeaveLiveRoom", CurrentRoom.Id, ct);
        CurrentRoom = null;
        Danmakus.Clear();
        Questions.Clear();
    }

    public async Task UpdateStepAsync(int stepIndex, CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        await _connection.InvokeAsync("UpdateLiveStep", CurrentRoom.Id, stepIndex, ct);
    }

    public async Task SendDanmakuAsync(string content, CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        await _connection.InvokeAsync("SendDanmaku", CurrentRoom.Id, content, ct);
    }

    public async Task AskQuestionAsync(string content, CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        await _connection.InvokeAsync("AskQuestion", CurrentRoom.Id, content, ct);
    }

    public async Task AnswerQuestionAsync(Guid questionId, string answer, CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        await _connection.InvokeAsync("AnswerQuestion", CurrentRoom.Id, questionId, answer, ct);
    }

    public async Task EndRoomAsync(CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        var roomId = CurrentRoom.Id;

        // 先本地清理，不依赖服务端事件回来才更新 UI
        LiveRooms.RemoveAll(r => r.Id == roomId);
        CurrentRoom = null;
        Danmakus.Clear();
        Questions.Clear();

        try { await _connection.InvokeAsync("EndLiveRoom", roomId, ct); }
        catch { }
    }

    public async Task RequestStreamAsync(CancellationToken ct = default)
    {
        if (_connection == null || CurrentRoom == null) return;
        await _connection.InvokeAsync("RequestStream", CurrentRoom.Id, ct);
    }

    public async Task SendOfferAsync(string viewerConnectionId, string sdp, CancellationToken ct = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("SendOffer", viewerConnectionId, sdp, ct);
    }

    public async Task SendAnswerAsync(string hostConnectionId, string sdp, CancellationToken ct = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("SendAnswer", hostConnectionId, sdp, ct);
    }

    public async Task RelayIceAsync(string targetConnectionId, string candidate, CancellationToken ct = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("RelayIce", targetConnectionId, candidate, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
