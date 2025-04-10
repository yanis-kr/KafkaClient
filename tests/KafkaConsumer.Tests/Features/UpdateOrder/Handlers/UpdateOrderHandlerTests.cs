using CloudNative.CloudEvents;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using KafkaConsumer.Tests.Fixtures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;


namespace KafkaConsumer.Tests.Features.UpdateOrder.Handlers;

public class UpdateOrderHandlerTests : IClassFixture<HostFixture>
{
    private readonly IHost _host;
    private readonly UpdateOrderHandler _handler;
    private readonly Mock<ILogger<UpdateOrderHandler>> _mockLogger;
    public UpdateOrderHandlerTests(HostFixture fixture)
    {
        // Set up the host with logging
        _host = fixture.TestHost;
        _mockLogger = new Mock<ILogger<UpdateOrderHandler>>();
        _handler = new UpdateOrderHandler(_mockLogger.Object);
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