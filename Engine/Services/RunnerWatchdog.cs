using Common.DTOs;

namespace Engine.Services;

public class RunnerWatchdog
{
    private readonly TimeSpan _checkInterval;
    private readonly Func<(bool, string)> _checkRestartCallback;
    private readonly TimeSpan _graceTime;
    private readonly List<string> _logLines = new();
    private readonly int _maxLogLines = 20;
    private readonly Action<string> _restartCallback;
    private readonly string _workerId;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _running;
    private Task? _watchdogTask;

    public RunnerWatchdog(string workerId, Func<(bool, string)> checkRestartCallback, Action<string> restartCallback,
        TimeSpan graceTime, TimeSpan checkInterval)
    {
        _workerId = workerId;
        _checkRestartCallback = checkRestartCallback;
        _restartCallback = restartCallback;
        _graceTime = graceTime;
        _checkInterval = checkInterval;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public event EventHandler<string>? StateChanged;

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
        _cancellationTokenSource.Cancel();

        if (_watchdogTask != null)
            try
            {
                // Vent på, at tasken afsluttes
                await _watchdogTask;
            }
            catch (OperationCanceledException)
            {
                AddLog("Watchdog task was cancelled.");
            }

        AddLog($"RunnerWatchdog: {_workerId} stopped.");
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        AddLog($"RunnerWatchdog: {_workerId} started with a grace time of {_graceTime}.");

        // Initial grace time before starting checks
        try
        {
            await Task.Delay(_graceTime, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            AddLog("Watchdog task was cancelled during grace time.");
            return;
        }

        AddLog($"RunnerWatchdog: {_workerId} finished grace time and is now monitoring every {_checkInterval}.");

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                await Task.Delay(_checkInterval, cancellationToken);

                var (needRestart, message) = _checkRestartCallback();
                if (needRestart)
                {
                    AddLog($"Watchdog detected a need for restart: {message}");
                    _restartCallback(message);
                    OnStateChanged($"Worker {_workerId} is restarting due to: {message}");
                }
                else
                {
                    AddLog("Watchdog check passed.");
                }
            }
            catch (TaskCanceledException)
            {
                AddLog("Watchdog task was cancelled.");
                break;
            }
    }

    public void OnRunnerLogGenerated(object? sender, WorkerLogEntry workerLog)
    {
        // Tilføj log fra runneren til logbufferen
        AddLog($"{workerLog.Timestamp}: {workerLog.Message}");
    }

    private void OnStateChanged(string message)
    {
        StateChanged?.Invoke(this, message);
    }

    private void AddLog(string message)
    {
        _logLines.Add($"{DateTime.UtcNow}: {message}");
        if (_logLines.Count > _maxLogLines) _logLines.RemoveAt(0);
    }

    public List<string> GetLogLines()
    {
        return new List<string>(_logLines);
    }
}