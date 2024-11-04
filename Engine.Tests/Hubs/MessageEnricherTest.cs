using System;
using Common.DTOs;
using Engine.Hubs;
using Xunit;

namespace Engine.Tests.Hubs;

public class MessageEnricherTest
{
    [Fact]
    public void Enrich_Should_Set_Correct_EngineId()
    {
        // Arrange
        var enricher = new MessageEnricher();
        var message = new BaseMessage();
        var engineId = Guid.NewGuid();

        // Act
        enricher.Enrich(message, engineId);

        // Assert
        Assert.Equal(engineId, message.EngineId);
    }

    [Fact]
    public void Enrich_Should_Increment_SequenceNumber()
    {
        // Arrange
        var enricher = new MessageEnricher();
        var message1 = new BaseMessage();
        var message2 = new BaseMessage();
        var engineId = Guid.NewGuid();

        // Act
        enricher.Enrich(message1, engineId);
        enricher.Enrich(message2, engineId);

        // Assert
        Assert.Equal(1, message1.SequenceNumber);
        Assert.Equal(2, message2.SequenceNumber);
    }

    [Fact]
    public void Enrich_Should_Set_Timestamp_To_Current_Time()
    {
        // Arrange
        var enricher = new MessageEnricher();
        var message = new BaseMessage();
        var engineId = Guid.NewGuid();
        var beforeEnrichment = DateTime.UtcNow;

        // Act
        enricher.Enrich(message, engineId);

        // Assert
        Assert.True(message.Timestamp >= beforeEnrichment && message.Timestamp <= DateTime.UtcNow);
    }
}