using Common.DTOs;
using Engine.Services;

namespace Engine.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Engine.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

public class MultiQueueTests
{
    private readonly Mock<ILogger<MultiQueue>> _loggerMock = new();

    [Fact]
    public void EnqueueMessage_ShouldAddMessageToDefaultQueue()
    {
        // Arrange
        var multiQueue = new MultiQueue(_loggerMock.Object);
        var message = new BaseMessage();

        // Act
        multiQueue.EnqueueMessage(message);

        // Assert
        Assert.Equal(1, multiQueue.GetQueueSizeForType(message.GetType()));
    }

    [Fact]
    public void EnqueueMessage_ShouldAddMessageToUniqueQueue_WithUniqueId()
    {
        // Arrange
        var multiQueue = new MultiQueue(_loggerMock.Object);
        var message = new BaseMessage();
        var uniqueId = "uniqueId";

        // Act
        multiQueue.EnqueueMessage(message, uniqueId);

        // Assert
        Assert.Equal(1, multiQueue.GetUniqueQueueSize(uniqueId));
    }

    [Fact]
    public void EnqueueMessage_ShouldKeepOnlyOneMessageInUniqueQueue()
    {
        // Arrange
        var multiQueue = new MultiQueue(_loggerMock.Object);
        var message1 = new BaseMessage();
        var message2 = new BaseMessage();
        var uniqueId = "uniqueId";

        // Act
        multiQueue.EnqueueMessage(message1, uniqueId);
        multiQueue.EnqueueMessage(message2, uniqueId);

        // Assert
        Assert.Equal(1, multiQueue.GetUniqueQueueSize(uniqueId)); // Kun én besked bør være i køen
    }

    [Fact]
    public void EnqueueMessage_ShouldRespectDefaultQueueLimit()
    {
        // Arrange
        var queueLimit = 3;
        var multiQueue = new MultiQueue(_loggerMock.Object, queueLimit);

        // Act
        for (int i = 0; i < queueLimit + 1; i++)
        {
            multiQueue.EnqueueMessage(new BaseMessage());
        }

        // Assert
        var messageType = typeof(BaseMessage);
        Assert.Equal(queueLimit, multiQueue.GetQueueSizeForType(messageType)); // Kun queueLimit beskeder bør være i køen
    }

    [Fact]
    public async Task DequeueMessageAsync_ShouldReturnNextAvailableMessage()
    {
        // Arrange
        var multiQueue = new MultiQueue(_loggerMock.Object);
        var message = new BaseMessage();
        multiQueue.EnqueueMessage(message);

        // Act
        var dequeuedMessage = await multiQueue.DequeueMessageAsync(CancellationToken.None);

        // Assert
        Assert.Equal(message, dequeuedMessage);
    }
}
