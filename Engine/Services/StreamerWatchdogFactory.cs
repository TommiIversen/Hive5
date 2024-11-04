namespace Engine.Services;

public interface IStreamerWatchdogFactory
{
    IStreamerWatchdogService CreateWatchdog(
        string workerId,
        Func<(bool, string)> checkRestartCallback,
        Func<string, Task> restartCallback,
        TimeSpan graceTime,
        TimeSpan checkInterval);
}

public class StreamerWatchdogFactory : IStreamerWatchdogFactory
{
    private readonly ILoggerService _loggerService;

    public StreamerWatchdogFactory(ILoggerService loggerService)
    {
        _loggerService = loggerService;
    }

    public IStreamerWatchdogService CreateWatchdog(
        string workerId,
        Func<(bool, string)> checkRestartCallback,
        Func<string, Task> restartCallback,
        TimeSpan graceTime,
        TimeSpan checkInterval)
    {
        return new StreamerWatchdogService(workerId, checkRestartCallback, restartCallback, graceTime, checkInterval,
            _loggerService);
    }
}