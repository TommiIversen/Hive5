using System;
using System.Threading;
using System.Threading.Tasks;
using Common.DTOs;
using Common.DTOs.Events;
using Engine.Interfaces;
using Engine.Services;
using Engine.Utils;
using Moq;
using Xunit;

public class MetricsServiceTests
{
    private readonly MetricsService _metricsService;
    private readonly Mock<IMessageQueue> _mockMessageQueue;
    private readonly Mock<INetworkInterfaceProvider> _mockNetworkInterfaceProvider;

    public MetricsServiceTests()
    {
        _mockMessageQueue = new Mock<IMessageQueue>();
        _mockNetworkInterfaceProvider = new Mock<INetworkInterfaceProvider>();
        _metricsService = new MetricsService(_mockMessageQueue.Object, _mockNetworkInterfaceProvider.Object,
            TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task GenerateMetricsAsync_ShouldEnqueueMetricInMessageQueue()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        _mockMessageQueue
            .Setup(mq => mq.EnqueueMessageAsync(It.IsAny<EngineMetric>(), cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _metricsService.GenerateMetricsAsync(cancellationToken);

        // Assert
        _mockMessageQueue.Verify(mq => mq.EnqueueMessageAsync(It.IsAny<EngineMetric>(), cancellationToken), Times.Once);
    }
}