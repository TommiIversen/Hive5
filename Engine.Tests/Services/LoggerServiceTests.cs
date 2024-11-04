using System.Linq;
using Common.DTOs;
using Engine.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ILogger = Serilog.ILogger;

namespace Engine.Services.Tests;

public class LoggerServiceTests
{
    private readonly LoggerService _loggerService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IMessageQueue> _mockMessageQueue;

    public LoggerServiceTests()
    {
        _mockMessageQueue = new Mock<IMessageQueue>();
        _mockLogger = new Mock<ILogger>();
        _loggerService = new LoggerService(_mockMessageQueue.Object, _mockLogger.Object);
    }

    [Fact]
    public void LogMessage_ShouldEnqueueMessageAndLog()
    {
        // Arrange
        var logEntry = new WorkerLogEntry
        {
            WorkerId = "worker1",
            Message = "Test message",
            LogLevel = LogLevel.Information
        };

        // Act
        _loggerService.LogMessage(logEntry);

        // Assert
        _mockMessageQueue.Verify(mq => mq.EnqueueMessage(logEntry), Times.Once);
        _mockLogger.Verify(logger => logger.Information(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void GetLastWorkerLogs_ShouldReturnLast20Logs()
    {
        // Arrange
        var workerId = "worker1";
        for (var i = 1; i <= 25; i++)
            _loggerService.LogMessage(new WorkerLogEntry
            {
                WorkerId = workerId,
                Message = $"Message {i}",
                LogLevel = LogLevel.Information
            });

        // Act
        var logs = _loggerService.GetLastWorkerLogs(workerId).ToList();

        // Assert
        Assert.Equal(20, logs.Count);
        Assert.Equal("Message 6", logs.First().Message);
        Assert.Equal("Message 25", logs.Last().Message);
    }

    [Fact]
    public void DeleteWorkerLogs_ShouldRemoveLogsAndCounterForWorker()
    {
        // Arrange
        var workerId = "worker1";
        _loggerService.LogMessage(new WorkerLogEntry
        {
            WorkerId = workerId,
            Message = "Test message",
            LogLevel = LogLevel.Information
        });

        // Act
        _loggerService.DeleteWorkerLogs(workerId);
        var logs = _loggerService.GetLastWorkerLogs(workerId);

        // Assert
        Assert.Empty(logs);
    }
}