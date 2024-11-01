using Common.DTOs;

namespace Engine.Services;

public interface IStreamerWatchdogService
{
    event StreamerWatchdogService.AsyncEventHandler<string>? StateChanged;
    Task StartAsync();
    Task StopAsync();
    void UpdateGraceTime(TimeSpan newGraceTime);
    void UpdateCheckInterval(TimeSpan newCheckInterval);
    void SetEnabled(bool isEnabled);
}

public class StreamerWatchdogService : IStreamerWatchdogService
{
    private TimeSpan _checkInterval;
    private readonly Func<(bool, string)> _checkRestartCallback;
    private TimeSpan _graceTime;
    private readonly Func<string, Task> _restartCallback;
    private readonly string _workerId;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _running;
    private bool _enabled = true;
    private Task? _watchdogTask;
    private readonly ILoggerService _loggerService;
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
    public event AsyncEventHandler<string>? StateChanged;

    public StreamerWatchdogService(
        string workerId,
        Func<(bool, string)> checkRestartCallback,
        Func<string, Task> restartCallback,
        TimeSpan graceTime,
        TimeSpan checkInterval,
        ILoggerService loggerService)
    {
        _workerId = workerId;
        _checkRestartCallback = checkRestartCallback;
        _restartCallback = restartCallback;
        _graceTime = graceTime;
        _checkInterval = checkInterval;
        _cancellationTokenSource = new CancellationTokenSource();
        _loggerService = loggerService;
    }

    public async Task StartAsync()
    {
        if (_running) await StopAsync();

        _running = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _watchdogTask = RunAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (!_running) return;

        _running = false;
        await _cancellationTokenSource.CancelAsync();

        if (_watchdogTask != null)
        {
            try
            {
                await _watchdogTask;
            }
            catch (OperationCanceledException)
            {
                LogInfo("Watchdog task was cancelled.");
            }
        }

        LogInfo($"RunnerWatchdog: {_workerId} stopped.");
    }

    public void UpdateGraceTime(TimeSpan newGraceTime)
    {
        _graceTime = newGraceTime;
        LogInfo($"Grace time updated to {_graceTime}");
    }

    public void UpdateCheckInterval(TimeSpan newCheckInterval)
    {
        _checkInterval = newCheckInterval;
        LogInfo($"Check interval updated to {_checkInterval}");
    }

    public void SetEnabled(bool isEnabled)
    {
        _enabled = isEnabled;
        LogInfo($"Watchdog enabled set to {_enabled}");
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        LogInfo($"RunnerWatchdog: {_workerId} started with a grace time of {_graceTime}.");
        try
        {
            await Task.Delay(_graceTime, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            LogInfo("Watchdog task was cancelled during grace time.");
            return;
        }

        LogInfo($"RunnerWatchdog: {_workerId} finished grace time and is now monitoring every {_checkInterval}.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, cancellationToken);

                if (!_enabled) 
                {
                    LogInfo("Watchdog check skipped because it is disabled.");
                    continue;
                }

                var (needRestart, message) = _checkRestartCallback();
                if (needRestart)
                {
                    LogInfo($"Watchdog detected a need for restart: {message}");
                    LogInfo("Calling _restartCallback...");
                    await ExecuteRestartCallbackAsync(message);
                    LogInfo("Finished _restartCallback.");
                }
                else
                {
                    LogInfo("Watchdog check passed.");
                }
            }
            catch (TaskCanceledException)
            {
                LogInfo("Watchdog task was cancelled.");
                break;
            }
        }
    }

    private async Task ExecuteRestartCallbackAsync(string message)
    {
        try
        {
            await _restartCallback(message);
            await OnStateChangedAsync($"Worker {_workerId} is restarting due to: {message}");
        }
        catch (Exception ex)
        {
            LogInfo($"Error during _restartCallback: {ex.Message}", LogLevel.Critical);
        }
    }

    private async Task OnStateChangedAsync(string message)
    {
        if (StateChanged != null)
        {
            var handlers = StateChanged.GetInvocationList();
            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                if (handler is AsyncEventHandler<string> asyncHandler)
                {
                    tasks.Add(asyncHandler(this, message));
                }
            }

            await Task.WhenAll(tasks);
        }
    }

    private void LogInfo(string message, LogLevel logLevel = LogLevel.Information)
    {
        _loggerService.LogMessage(new WorkerLogEntry
        {
            WorkerId = _workerId,
            Message = $"WatchDog: {message}",
            LogLevel = logLevel
        });
    }

}