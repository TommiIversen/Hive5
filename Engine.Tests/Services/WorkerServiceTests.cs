using System;
using System.Threading.Tasks;
using Common.DTOs;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.DAL.Entities;
using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Engine.Tests.Services;

public class WorkerServiceTests
{
    private readonly WorkerConfiguration _config;
    private readonly Mock<ILoggerService> _loggerServiceMock = new();
    private readonly Mock<IMessageQueue> _messageQueueMock = new();
    private readonly Mock<IRepositoryFactory> _repositoryFactoryMock = new();
    private readonly Mock<IStreamerService> _streamerServiceMock = new();
    private readonly Mock<IStreamerWatchdogFactory> _watchdogFactoryMock = new();
    private readonly Mock<IStreamerWatchdogService> _watchdogServiceMock = new();

    public WorkerServiceTests()
    {
        _config = new WorkerConfiguration
        {
            WorkerId = "test-worker",
            GstCommand = "test-gst-command",
            ImgWatchdogEnabled = true,
            ImgWatchdogGraceTime = TimeSpan.FromSeconds(10),
            ImgWatchdogInterval = TimeSpan.FromSeconds(5)
        };

        _watchdogFactoryMock
            .Setup(factory => factory.CreateWatchdog(It.IsAny<string>(), It.IsAny<Func<(bool, string)>>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
            .Returns(_watchdogServiceMock.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldSetDesiredStateToRunning()
    {
        // Arrange
        _streamerServiceMock.Setup(s => s.GetState()).Returns(WorkerState.Idle);
        _streamerServiceMock.Setup(s => s.StartAsync()).ReturnsAsync((WorkerState.Running, "Started successfully"));

        var workerService = new WorkerService(_loggerServiceMock.Object, _messageQueueMock.Object,
            _streamerServiceMock.Object,
            _repositoryFactoryMock.Object, _watchdogFactoryMock.Object, _config);

        // Act
        var result = await workerService.StartAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Started successfully", result.Message);
    }

    [Fact]
    public async Task StopAsync_ShouldSetDesiredStateToIdle()
    {
        // Arrange
        _streamerServiceMock.Setup(s => s.GetState()).Returns(WorkerState.Running);
        _streamerServiceMock.Setup(s => s.StopAsync()).ReturnsAsync((WorkerState.Idle, "Stopped successfully"));

        var workerService = new WorkerService(_loggerServiceMock.Object, _messageQueueMock.Object,
            _streamerServiceMock.Object,
            _repositoryFactoryMock.Object, _watchdogFactoryMock.Object, _config);

        // Act
        var result = await workerService.StopAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Stopped successfully", result.Message);
    }


    [Fact]
    public void SetGstCommand_ShouldUpdateStreamerServiceGstCommand()
    {
        // Arrange
        var workerService = new WorkerService(_loggerServiceMock.Object, _messageQueueMock.Object,
            _streamerServiceMock.Object,
            _repositoryFactoryMock.Object, _watchdogFactoryMock.Object, _config);
        var newGstCommand = "new-gst-command";

        // Act
        workerService.SetGstCommand(newGstCommand);

        // Assert
        _streamerServiceMock.VerifySet(s => s.GstCommand = newGstCommand, Times.Once);
    }


    [Fact]
    public async Task HandleStateChangeAsync_ShouldEnqueueStateChangeEvent()
    {
        // Arrange
        var workerRepositoryMock = new Mock<IWorkerRepository>();
        var workerEntity = new WorkerEntity
        {
            Name = "kk",
            WorkerId = _config.WorkerId,
            ImgWatchdogGraceTime = default,
            ImgWatchdogInterval = default
        };

        // Konfigurer mock for repository og entity
        workerRepositoryMock.Setup(repo => repo.GetWorkerByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(workerEntity);
        workerRepositoryMock.Setup(repo => repo.UpdateWorkerAsync(It.IsAny<WorkerEntity>()));
        _repositoryFactoryMock.Setup(factory => factory.CreateWorkerRepository())
            .Returns(workerRepositoryMock.Object);

        var workerService = new WorkerService(_loggerServiceMock.Object, _messageQueueMock.Object,
            _streamerServiceMock.Object,
            _repositoryFactoryMock.Object, _watchdogFactoryMock.Object, _config);

        // Act
        await workerService.HandleStateChangeAsync(WorkerState.Idle);

        // Assert
        _messageQueueMock.Verify(q => q.EnqueueMessage(It.IsAny<BaseMessage>()), Times.Once);
    }


    [Fact]
    public void LogInfo_ShouldLogMessageUsingLoggerService()
    {
        // Arrange
        var workerService = new WorkerService(_loggerServiceMock.Object, _messageQueueMock.Object,
            _streamerServiceMock.Object,
            _repositoryFactoryMock.Object, _watchdogFactoryMock.Object, _config);
        var message = "Test log message";

        // Act
        workerService.LogInfo(message, LogLevel.Warning);

        // Assert
        _loggerServiceMock.Verify(l => l.LogMessage(It.Is<WorkerLogEntry>(entry =>
            entry.Message.Contains(message) && entry.LogLevel == LogLevel.Warning)), Times.Once);
    }
}