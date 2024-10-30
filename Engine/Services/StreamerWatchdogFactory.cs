namespace Engine.Services;

public class StreamerWatchdogFactory
{
    public IStreamerWatchdogService CreateWatchdog(
        string workerId, 
        Func<(bool, string)> checkRestartCallback, 
        Func<string, Task> restartCallback, 
        TimeSpan graceTime, 
        TimeSpan checkInterval)
    {
        return new StreamerWatchdogService(workerId, checkRestartCallback, restartCallback, graceTime, checkInterval);
    }
}