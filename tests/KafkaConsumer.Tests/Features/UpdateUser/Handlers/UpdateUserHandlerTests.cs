using CloudNative.CloudEvents;
using KafkaConsumer.Features.UpdateUser.Handlers;
using System;
using Xunit;

namespace KafkaConsumer.Tests.Features.UpdateUser.Handlers;

public class UpdateUserHandlerTests
{
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTests()
    {
        _handler = new UpdateUserHandler();
    }

    [Fact]
    public void Name_ReturnsCorrectHandlerName()
    {
        // Act
        var name = _handler.Name;

        // Assert
        Assert.Equal("UpdateUser", name);
    }

    [Fact]
    public void ProcessEvent_WithValidEvent_ReturnsTrue()
    {
        // Arrange
        var cloudEvent = new CloudEvent
        {
            Type = "user.created",
            Id = "test-id",
            Data = "{\"accountId\": \"123\", \"customerName\": \"John Doe\", \"date\": \"2024-04-09\"}"
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