namespace KitchenAssistant.Core.Models;

public class CookingSession
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public string HostUserId { get; set; } = string.Empty;
    public string HostUserName { get; set; } = string.Empty;
    public List<SessionParticipant> Participants { get; set; } = [];
    public List<TaskAssignment> TaskAssignments { get; set; } = [];
    public int CurrentStepIndex { get; set; }
    public CookingSessionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class SessionParticipant
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsOnline { get; set; }
}

public class TaskAssignment
{
    public Guid Id { get; set; }
    public int StepIndex { get; set; }
    public string StepDescription { get; set; } = string.Empty;
    public string? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum CookingSessionStatus
{
    Preparing,
    Cooking,
    Paused,
    Completed,
    Cancelled
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed
}
