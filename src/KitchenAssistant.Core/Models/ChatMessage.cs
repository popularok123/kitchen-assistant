namespace KitchenAssistant.Core.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }
    public string? TargetUserName { get; set; }
}

public enum MessageType
{
    Normal,
    System,
    CookingProgress,
    Private
}
