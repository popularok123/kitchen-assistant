using Microsoft.AspNetCore.SignalR.Client;
using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Web.Services;

public class ChatService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;
    public List<ChatMessage> Messages { get; } = [];
    public List<User> OnlineUsers { get; } = [];
    public List<CookingSession> ActiveSessions { get; } = [];

    public event Action? OnUpdate;

    public ChatService(string hubUrl = "http://localhost:5001/cookingHub")
    {
        _hubUrl = hubUrl;
    }

    public async Task ConnectAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (_connection != null)
            await _connection.DisposeAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .Build();

        _connection.On<ChatMessage>("ReceiveMessage", msg =>
        {
            Messages.Add(msg);
            if (Messages.Count > 50)
                Messages.RemoveAt(0);
            OnUpdate?.Invoke();
        });

        _connection.On<User>("UserJoined", user =>
        {
            if (!OnlineUsers.Any(u => u.ConnectionId == user.ConnectionId))
                OnlineUsers.Add(user);
            OnUpdate?.Invoke();
        });

        _connection.On<User>("UserLeft", user =>
        {
            OnlineUsers.RemoveAll(u => u.ConnectionId == user.ConnectionId);
            OnUpdate?.Invoke();
        });

        _connection.On<List<User>>("OnlineUsers", users =>
        {
            OnlineUsers.Clear();
            OnlineUsers.AddRange(users);
            OnUpdate?.Invoke();
        });

        _connection.On<List<CookingSession>>("ActiveSessions", sessions =>
        {
            ActiveSessions.Clear();
            ActiveSessions.AddRange(sessions);
            OnUpdate?.Invoke();
        });

        _connection.On<List<ChatMessage>>("MessageHistory", history =>
        {
            Messages.Clear();
            Messages.AddRange(history);
            OnUpdate?.Invoke();
        });

        _connection.On<Guid>("MessageRecalled", msgId =>
        {
            Messages.RemoveAll(m => m.Id == msgId);
            OnUpdate?.Invoke();
        });

        _connection.On<CookingSession>("SessionCreated", session =>
        {
            if (!ActiveSessions.Any(s => s.Id == session.Id))
                ActiveSessions.Add(session);
            OnUpdate?.Invoke();
        });

        _connection.On<CookingSession>("SessionUpdated", session =>
        {
            var index = ActiveSessions.FindIndex(s => s.Id == session.Id);
            if (index >= 0)
                ActiveSessions[index] = session;
            else
                ActiveSessions.Add(session);
            OnUpdate?.Invoke();
        });

        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync("JoinRoom", userName, cancellationToken);
    }

    public async Task SendMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("SendMessage", content, cancellationToken);
    }

    public async Task RecallMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("RecallMessage", messageId, cancellationToken);
    }

    public async Task SendPrivateMessageAsync(string targetUserName, string content, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("SendPrivateMessage", targetUserName, content, cancellationToken);
    }

    public async Task<Guid> CreateCookingSessionAsync(string recipeName, Guid recipeId, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return Guid.Empty;
        return await _connection.InvokeAsync<Guid>("CreateCookingSession", recipeName, recipeId, cancellationToken);
    }

    public async Task JoinCookingSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("JoinCookingSession", sessionId, cancellationToken);
    }

    public async Task AssignTaskAsync(Guid sessionId, int stepIndex, string stepDescription, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("AssignTask", sessionId, stepIndex, stepDescription, cancellationToken);
    }

    public async Task CompleteTaskAsync(Guid sessionId, int stepIndex, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        await _connection.InvokeAsync("CompleteTask", sessionId, stepIndex, cancellationToken);
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
