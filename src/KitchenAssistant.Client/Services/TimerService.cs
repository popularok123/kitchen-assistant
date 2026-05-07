using KitchenAssistant.Core.Services;

namespace KitchenAssistant.Client.Services;

public class TimerService : ITimerService, IDisposable
{
    private readonly Dictionary<Guid, TimerInstance> _timers = new();
    private readonly PeriodicTimer _tickTimer;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public event EventHandler<TimerEventArgs>? TimerCompleted;
    public event EventHandler<TimerEventArgs>? TimerTick;

    public TimerService()
    {
        _tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = RunTickLoop();
    }

    public Guid StartTimer(string name, int durationSeconds, Action<Guid, string>? onCompleted = null)
    {
        var timerId = Guid.NewGuid();
        var timer = new TimerInstance
        {
            Id = timerId,
            Name = name,
            TotalSeconds = durationSeconds,
            RemainingSeconds = durationSeconds,
            IsRunning = true,
            StartTime = DateTime.Now,
            OnCompleted = onCompleted
        };

        lock (_timers)
        {
            _timers[timerId] = timer;
        }

        return timerId;
    }

    public void StopTimer(Guid timerId)
    {
        lock (_timers)
        {
            _timers.Remove(timerId);
        }
    }

    public void PauseTimer(Guid timerId)
    {
        lock (_timers)
        {
            if (_timers.TryGetValue(timerId, out var timer))
            {
                timer.IsRunning = false;
            }
        }
    }

    public void ResumeTimer(Guid timerId)
    {
        lock (_timers)
        {
            if (_timers.TryGetValue(timerId, out var timer))
            {
                timer.IsRunning = true;
            }
        }
    }

    public List<CookingTimer> GetActiveTimers()
    {
        lock (_timers)
        {
            return _timers.Values.Select(t => t.ToCookingTimer()).ToList();
        }
    }

    public CookingTimer? GetTimer(Guid timerId)
    {
        lock (_timers)
        {
            if (_timers.TryGetValue(timerId, out var timer))
            {
                return timer.ToCookingTimer();
            }
        }
        return null;
    }

    private async Task RunTickLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _tickTimer.WaitForNextTickAsync(_cts.Token);
                await ProcessTickAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private Task ProcessTickAsync()
    {
        var completedTimers = new List<TimerInstance>();

        lock (_timers)
        {
            foreach (var timer in _timers.Values.ToList())
            {
                if (!timer.IsRunning || timer.IsCompleted)
                    continue;

                timer.RemainingSeconds--;

                if (timer.RemainingSeconds <= 0)
                {
                    timer.IsCompleted = true;
                    timer.IsRunning = false;
                    completedTimers.Add(timer);
                }
            }
        }

        foreach (var timer in completedTimers)
        {
            TimerCompleted?.Invoke(this, new TimerEventArgs
            {
                TimerId = timer.Id,
                TimerName = timer.Name,
                RemainingSeconds = 0
            });

            timer.OnCompleted?.Invoke(timer.Id, timer.Name);

            lock (_timers)
            {
                _timers.Remove(timer.Id);
            }
        }

        var activeTimers = GetActiveTimers();
        foreach (var timer in activeTimers)
        {
            TimerTick?.Invoke(this, new TimerEventArgs
            {
                TimerId = timer.Id,
                TimerName = timer.Name,
                RemainingSeconds = timer.RemainingSeconds
            });
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cts.Cancel();
        _cts.Dispose();
        _tickTimer.Dispose();
        _disposed = true;
    }

    private class TimerInstance
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalSeconds { get; set; }
        public int RemainingSeconds { get; set; }
        public bool IsRunning { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public Action<Guid, string>? OnCompleted { get; set; }

        public CookingTimer ToCookingTimer() => new()
        {
            Id = Id,
            Name = Name,
            TotalSeconds = TotalSeconds,
            RemainingSeconds = RemainingSeconds,
            IsRunning = IsRunning,
            IsCompleted = IsCompleted,
            StartTime = StartTime
        };
    }
}
