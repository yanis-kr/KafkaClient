using CloudNative.CloudEvents;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using System;
using Xunit;

namespace KafkaConsumer.Tests.Features.UpdateOrder.Handlers;

public class UpdateOrderHandlerTests
{
    private readonly UpdateOrderHandler _handler;

    public UpdateOrderHandlerTests()
    {
        _handler = new UpdateOrderHandler();
    }

    [Fact]
    public void Name_ReturnsCorrectHandlerName()
    {
        // Act
        var name = _handler.Name;

        // Assert
        Assert.Equal("UpdateOrder", name);
    }

    [Fact]
    public void ProcessEvent_WithValidEvent_ReturnsTrue()
    {
        // Arrange
        var cloudEvent = new CloudEvent
        {
            Type = "order.created",
            Id = "test-id",
            Data = "{\"orderId\": \"123\", \"status\": \"created\"}"
        };

        // Act
        var result = _handler.ProcessEvent(cloudEvent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ProcessEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.ProcessEvent(null));
    }
} 