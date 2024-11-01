using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Engine.Services;
using Xunit;

public class StreamerWatchdogServiceTests
{
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<Func<(bool, string)>> _mockCheckRestartCallback;
    private readonly Mock<Func<string, Task>> _mockRestartCallback;
    private readonly StreamerWatchdogService _watchdogService;

    public StreamerWatchdogServiceTests()
    {
        _mockLoggerService = new Mock<ILoggerService>();
        _mockCheckRestartCallback = new Mock<Func<(bool, string)>>();
        _mockRestartCallback = new Mock<Func<string, Task>>();
        
        // Initialiserer StreamerWatchdogService med mock-objekter og reducerede tidsintervaller
        _watchdogService = new StreamerWatchdogService(
            "test-worker",
            _mockCheckRestartCallback.Object,
            _mockRestartCallback.Object,
            TimeSpan.FromMilliseconds(100),  // Grace time reduceret til 100 ms
            TimeSpan.FromMilliseconds(50),   // Check interval reduceret til 50 ms
            _mockLoggerService.Object);
    }

    [Fact]
    public async Task StartAsync_StartsWatchdogTask()
    {
        // Act
        await _watchdogService.StartAsync();

        // Assert
        Assert.True(_watchdogService.IsRunning);

        // Cleanup
        await _watchdogService.StopAsync();
    }

    [Fact]
    public async Task StopAsync_StopsWatchdogTask()
    {
        // Arrange
        await _watchdogService.StartAsync();

        // Act
        await _watchdogService.StopAsync();

        // Assert
        Assert.False(_watchdogService.IsRunning);
    }

    [Fact]
    public async Task Watchdog_PerformsRestart_WhenCheckRestartCallbackReturnsTrue()
    {
        // Arrange
        _mockCheckRestartCallback.Setup(callback => callback()).Returns((true, "Restart needed"));
        _mockRestartCallback.Setup(callback => callback(It.IsAny<string>())).Returns(Task.CompletedTask);

        await _watchdogService.StartAsync();

        // Act
        await Task.Delay(200); // Reduceret delay for at tillade flere loops i watchdog-løkken

        // Assert
        _mockRestartCallback.Verify(callback => callback(It.IsAny<string>()), Times.AtLeastOnce);
    
        // Cleanup
        await _watchdogService.StopAsync();
    }


    [Fact]
    public async Task Watchdog_RaisesStateChangedEvent_WhenRestartIsTriggered()
    {
        // Arrange
        var eventRaised = new ManualResetEventSlim(false);
        _watchdogService.StateChanged += async (sender, e) => eventRaised.Set();

        _mockCheckRestartCallback.Setup(callback => callback()).Returns((true, "Restart triggered"));
        _mockRestartCallback.Setup(callback => callback(It.IsAny<string>())).Returns(Task.CompletedTask);

        await _watchdogService.StartAsync();

        // Act
        bool eventWasRaised = eventRaised.Wait(200); // Vent op til 200 ms på, at eventen udløses

        // Assert
        Assert.True(eventWasRaised, "StateChanged event was not raised within the expected time.");

        // Cleanup
        await _watchdogService.StopAsync();
    }


    [Fact]
    public void UpdateGraceTime_ChangesGraceTime()
    {
        // Act
        _watchdogService.UpdateGraceTime(TimeSpan.FromMilliseconds(200)); // Reduceret grace time

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(200), _watchdogService.GraceTime);
    }

    [Fact]
    public void UpdateCheckInterval_ChangesCheckInterval()
    {
        // Act
        _watchdogService.UpdateCheckInterval(TimeSpan.FromMilliseconds(75)); // Reduceret check interval

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(75), _watchdogService.CheckInterval);
    }
}
