namespace KitchenAssistant.Core.Models;

public class LiveRoom
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HostConnectionId { get; set; } = string.Empty;
    public string HostUserName { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public int CurrentStepIndex { get; set; }
    public List<LiveViewer> Viewers { get; set; } = [];
    public List<LiveQuestion> Questions { get; set; } = [];
    public DateTime StartTime { get; set; }
    public bool IsLive { get; set; }
}

public class LiveViewer
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class LiveQuestion
{
    public Guid Id { get; set; }
    public string AskUserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public DateTime AskedAt { get; set; }
    public bool IsAnswered { get; set; }
}

public class Danmaku
{
    public Guid RoomId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
