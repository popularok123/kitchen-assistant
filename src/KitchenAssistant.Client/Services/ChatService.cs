using Microsoft.AspNetCore.SignalR.Client;
using KitchenAssistant.Core.Models;

namespace KitchenAssistant.Client.Services;

public class ChatService : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private bool _connected;

    public event Action<ChatMessage>? MessageReceived;
    public event Action<User>? UserJoined;
    public event Action<User>? UserLeft;
    public event Action<List<User>>? OnlineUsersUpdated;
    public event Action<CookingSession>? SessionCreated;
    public event Action<CookingSession>? SessionUpdated;
    public event Action<List<CookingSession>>? ActiveSessionsReceived;

    public List<CookingSession> ActiveSessions { get; private set; } = [];
    public CookingSession? CurrentSession { get; private set; }
    public List<User> OnlineUsers { get; private set; } = [];
    public bool IsConnected => _connected;

    public ChatService(string serverUrl = "http://localhost:5000/cookingHub")
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .Build();

        _connection.On<ChatMessage>("ReceiveMessage", msg => MessageReceived?.Invoke(msg));
        _connection.On<User>("UserJoined", user => UserJoined?.Invoke(user));
        _connection.On<User>("UserLeft", user => UserLeft?.Invoke(user));
        _connection.On<List<User>>("OnlineUsers", users =>
        {
            OnlineUsers = users;
            OnlineUsersUpdated?.Invoke(users);
        });
        _connection.On<List<CookingSession>>("ActiveSessions", sessions =>
        {
            ActiveSessions = sessions;
            ActiveSessionsReceived?.Invoke(sessions);
        });
        _connection.On<CookingSession>("SessionCreated", session =>
        {
            ActiveSessions.Add(session);
            SessionCreated?.Invoke(session);
        });
        _connection.On<CookingSession>("SessionUpdated", session =>
        {
            var idx = ActiveSessions.FindIndex(s => s.Id == session.Id);
            if (idx >= 0) ActiveSessions[idx] = session;
            if (CurrentSession?.Id == session.Id) CurrentSession = session;
            SessionUpdated?.Invoke(session);
        });
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connected) return;
        await _connection.StartAsync(cancellationToken);
        _connected = true;
    }

    public async Task JoinRoomAsync(string userName, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("JoinRoom", userName, cancellationToken);
    }

    public async Task SendMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("SendMessage", content, cancellationToken);
    }

    public async Task BroadcastCookingProgressAsync(string recipeName, int currentStep, string stepDescription, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("BroadcastCookingProgress", recipeName, currentStep, stepDescription, cancellationToken);
    }

    public async Task<Guid> CreateCookingSessionAsync(string recipeName, Guid recipeId, CancellationToken cancellationToken = default)
    {
        var sessionId = await _connection.InvokeAsync<Guid>("CreateCookingSession", recipeName, recipeId, cancellationToken);
        return sessionId;
    }

    public async Task JoinCookingSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("JoinCookingSession", sessionId, cancellationToken);
        CurrentSession = ActiveSessions.FirstOrDefault(s => s.Id == sessionId);
    }

    public async Task AssignTaskAsync(Guid sessionId, int stepIndex, string stepDescription, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("AssignTask", sessionId, stepIndex, stepDescription, cancellationToken);
    }

    public async Task CompleteTaskAsync(Guid sessionId, int stepIndex, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("CompleteTask", sessionId, stepIndex, cancellationToken);
    }

    public async Task UpdateSessionStepAsync(Guid sessionId, int currentStep, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync("UpdateSessionStep", sessionId, currentStep, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connected)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
