﻿using Common.DTOs.Events;

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
    private const int MinimumIntervalSeconds = 1; // Minimumgrænse for tid

    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

    private readonly Func<(bool, string)> _checkRestartCallback;
    private readonly ILoggerService _loggerService;
    private readonly Func<string, Task> _restartCallback;
    private readonly string _workerId;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _enabled = true;
    private Task? _watchdogTask;

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
        GraceTime = ValidateTime(graceTime, "GraceTime");
        CheckInterval = ValidateTime(checkInterval, "CheckInterval");
        
        _cancellationTokenSource = new CancellationTokenSource();
        _loggerService = loggerService;
    }

    // Offentlig egenskab for at teste om watchdoggen kører
    public bool IsRunning { get; private set; }

    // Offentlig egenskab for at tilgå grace time
    public TimeSpan GraceTime { get; private set; }

    // Offentlig egenskab for at tilgå check interval
    public TimeSpan CheckInterval { get; private set; }

    public event AsyncEventHandler<string>? StateChanged;

    public async Task StartAsync()
    {
        if (IsRunning) await StopAsync();

        IsRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _watchdogTask = RunAsync(_cancellationTokenSource.Token);
    }


    public async Task StopAsync()
    {
        if (!IsRunning) return;

        IsRunning = false;
        await _cancellationTokenSource.CancelAsync();

        if (_watchdogTask != null)
            try
            {
                await _watchdogTask;
            }
            catch (OperationCanceledException)
            {
                LogInfo("Watchdog task was cancelled.");
            }

        LogInfo($"RunnerWatchdog: {_workerId} stopped.");
    }

    public void UpdateGraceTime(TimeSpan newGraceTime)
    {
        GraceTime = ValidateTime(newGraceTime, "GraceTime");
        LogInfo($"Grace time updated to {GraceTime}");
    }

    public void UpdateCheckInterval(TimeSpan newCheckInterval)
    {
        CheckInterval = ValidateTime(newCheckInterval, "CheckInterval");
        LogInfo($"Check interval updated to {CheckInterval}");
    }

    public void SetEnabled(bool isEnabled)
    {
        _enabled = isEnabled;
        LogInfo($"Watchdog enabled set to {_enabled}");
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        LogInfo($"RunnerWatchdog: {_workerId} started with a grace time of {GraceTime}.");
        try
        {
            await Task.Delay(GraceTime, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            LogInfo("Watchdog task was cancelled during grace time.");
            return;
        }

        LogInfo($"RunnerWatchdog: {_workerId} finished grace time and is now monitoring every {CheckInterval}.");

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                await Task.Delay(CheckInterval, cancellationToken);

                if (!_enabled)
                {
                    LogInfo("Watchdog check skipped because it is disabled.");
                    continue;
                }

                var (needRestart, message) = _checkRestartCallback();
                if (!needRestart) continue;

                LogInfo($"Watchdog detected a need for restart: {message}", LogLevel.Critical);
                LogInfo("Calling _restartCallback...");
                await ExecuteRestartCallbackAsync(message);
                LogInfo("Finished _restartCallback.");
            }
            catch (TaskCanceledException)
            {
                LogInfo("Watchdog task was cancelled.");
                break;
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
                if (handler is AsyncEventHandler<string> asyncHandler)
                    tasks.Add(asyncHandler(this, message));

            await Task.WhenAll(tasks);
        }
    }
    
    private TimeSpan ValidateTime(TimeSpan time, string propertyName)
    {
        if (time.TotalSeconds < MinimumIntervalSeconds)
        {
            return TimeSpan.FromSeconds(MinimumIntervalSeconds);
        }
        return time;
    }

    private void LogInfo(string message, LogLevel logLevel = LogLevel.Information)
    {
        _loggerService.LogMessage(new WorkerLogEntry
        {
            WorkerId = _workerId,
            Message = $"WatchDog: {message}",
            LogLevel = logLevel,
            LogTimestamp = DateTime.UtcNow
        });
    }
}