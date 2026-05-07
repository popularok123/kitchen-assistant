namespace KitchenAssistant.Core.Services;

public interface ITimerService
{
    Guid StartTimer(string name, int durationSeconds, Action<Guid, string>? onCompleted = null);
    void StopTimer(Guid timerId);
    void PauseTimer(Guid timerId);
    void ResumeTimer(Guid timerId);
    List<CookingTimer> GetActiveTimers();
    CookingTimer? GetTimer(Guid timerId);
    event EventHandler<TimerEventArgs>? TimerCompleted;
    event EventHandler<TimerEventArgs>? TimerTick;
}

public class CookingTimer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSeconds { get; set; }
    public int RemainingSeconds { get; set; }
    public bool IsRunning { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime StartTime { get; set; }
}

public class TimerEventArgs : EventArgs
{
    public Guid TimerId { get; set; }
    public string TimerName { get; set; } = string.Empty;
    public int RemainingSeconds { get; set; }
}
