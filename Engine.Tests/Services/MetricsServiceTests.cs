using System;
using System.Threading;
using System.Threading.Tasks;
using Common.DTOs;
using Engine.Interfaces;
using Engine.Services;
using Engine.Utils;
using Moq;
using Xunit;

public class MetricsServiceTests
{
    private readonly Mock<IMessageQueue> _mockMessageQueue;
    private readonly Mock<INetworkInterfaceProvider> _mockNetworkInterfaceProvider;
    private readonly MetricsService _metricsService;

    public MetricsServiceTests()
    {
        _mockMessageQueue = new Mock<IMessageQueue>();
        _mockNetworkInterfaceProvider = new Mock<INetworkInterfaceProvider>();
        _metricsService = new MetricsService(_mockMessageQueue.Object, _mockNetworkInterfaceProvider.Object, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task GenerateMetricsAsync_ShouldEnqueueMetricInMessageQueue()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        _mockMessageQueue
            .Setup(mq => mq.EnqueueMessageAsync(It.IsAny<Metric>(), cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _metricsService.GenerateMetricsAsync(cancellationToken);

        // Assert
        _mockMessageQueue.Verify(mq => mq.EnqueueMessageAsync(It.IsAny<Metric>(), cancellationToken), Times.Once);
    }
}