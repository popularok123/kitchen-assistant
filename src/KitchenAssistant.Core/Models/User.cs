namespace KitchenAssistant.Core.Models;

public class User
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime JoinedAt { get; set; }
}
