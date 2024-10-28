using Common.DTOs;

namespace Engine.Services;

public class StreamerWatchdogService
{
    private readonly TimeSpan _checkInterval;
    private readonly Func<(bool, string)> _checkRestartCallback;
    private readonly TimeSpan _graceTime;
    private readonly List<string> _logLines = new();
    private readonly int _maxLogLines = 20;
    private readonly Func<string, Task> _restartCallback; // Asynkron callback
    private readonly string _workerId;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _running;
    private Task? _watchdogTask;

    public StreamerWatchdogService(string workerId, Func<(bool, string)> checkRestartCallback, Func<string, Task> restartCallback,
        TimeSpan graceTime, TimeSpan checkInterval)
    {
        _workerId = workerId;
        _checkRestartCallback = checkRestartCallback;
        _restartCallback = restartCallback;
        _graceTime = graceTime;
        _checkInterval = checkInterval;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
    public event AsyncEventHandler<string>? StateChanged;

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
                AddLog("Watchdog task was cancelled.");
            }
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
        {
            try
            {
                await Task.Delay(_checkInterval, cancellationToken);

                var (needRestart, message) = _checkRestartCallback();
                if (needRestart)
                {
                    AddLog($"Watchdog detected a need for restart: {message}");

                    // Tilføj logging før og efter kaldet til _restartCallback for at diagnosticere problemet
                    AddLog("Calling _restartCallback...");
                    await ExecuteRestartCallbackAsync(message);
                    AddLog("Finished _restartCallback.");
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
    }

    private async Task ExecuteRestartCallbackAsync(string message)
    {
        try
        {
            // Udfør _restartCallback og håndter eventuelle undtagelser
            await _restartCallback(message);
            await OnStateChangedAsync($"Worker {_workerId} is restarting due to: {message}");
        }
        catch (Exception ex)
        {
            AddLog($"Error during _restartCallback: {ex.Message}");
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

    public void OnServiceLogGenerated(object? sender, WorkerLogEntry workerLog)
    {
        AddLog($"{workerLog.Timestamp}: {workerLog.Message}");
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
