using System.Threading;
using System.Threading.Tasks;
using Common.DTOs;
using Engine.Services;
using Xunit;

namespace Engine.Tests.Services;

public class MessageQueueTests
{
    [Fact]
    public void EnqueueMessage_ShouldAddMessageToQueue()
    {
        // Arrange
        var messageQueue = new MessageQueue(5);
        var message = new BaseMessage();

        // Act
        messageQueue.EnqueueMessage(message);

        // Assert
        Assert.Equal(1, messageQueue.GetQueueSize());
    }

    [Fact]
    public void EnqueueMessage_ShouldDropOldestWhenQueueIsFull()
    {
        // Arrange
        var messageQueue = new MessageQueue(3);
        var message1 = new BaseMessage();
        var message2 = new BaseMessage();
        var message3 = new BaseMessage();
        var message4 = new BaseMessage();

        // Act
        messageQueue.EnqueueMessage(message1);
        messageQueue.EnqueueMessage(message2);
        messageQueue.EnqueueMessage(message3);
        messageQueue.EnqueueMessage(message4); // Denne skal fjerne message1

        // Assert
        Assert.Equal(3, messageQueue.GetQueueSize());
    }

    [Fact]
    public async Task EnqueueMessageAsync_ShouldAddMessageToQueue()
    {
        // Arrange
        var messageQueue = new MessageQueue(5);
        var message = new BaseMessage();
        var cancellationToken = CancellationToken.None;

        // Act
        await messageQueue.EnqueueMessageAsync(message, cancellationToken);

        // Assert
        Assert.Equal(1, messageQueue.GetQueueSize());
    }

    [Fact]
    public async Task DequeueMessageAsync_ShouldReturnMessageFromQueue()
    {
        // Arrange
        var messageQueue = new MessageQueue(5);
        var message = new BaseMessage();
        var cancellationToken = CancellationToken.None;
        await messageQueue.EnqueueMessageAsync(message, cancellationToken);

        // Act
        var dequeuedMessage = await messageQueue.DequeueMessageAsync(cancellationToken);

        // Assert
        Assert.Equal(message, dequeuedMessage);
    }

    [Fact]
    public async Task GetQueueSize_ShouldReturnCorrectSize()
    {
        // Arrange
        var messageQueue = new MessageQueue(5);
        var message1 = new BaseMessage();
        var message2 = new BaseMessage();
        var cancellationToken = CancellationToken.None;

        // Act
        await messageQueue.EnqueueMessageAsync(message1, cancellationToken);
        await messageQueue.EnqueueMessageAsync(message2, cancellationToken);

        // Assert
        Assert.Equal(2, messageQueue.GetQueueSize());
    }
}