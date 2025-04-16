using CloudNative.CloudEvents;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KafkaConsumer.Tests.Common.Services;

public class EventDispatcherTests
{
    private readonly Mock<IOptions<TopicSettings>> _mockConfigOptions;
    private readonly Mock<IEventHandler> _mockUpdateOrderHandler;
    private readonly Mock<IEventHandler> _mockUpdateUserHandler;
    private readonly Mock<ILogger<EventDispatcher>> _mockLogger;
    private readonly TopicSettings _config;

    public EventDispatcherTests()
    {
        _mockUpdateOrderHandler = new Mock<IEventHandler>();
        _mockUpdateUserHandler = new Mock<IEventHandler>();
        _mockLogger = new Mock<ILogger<EventDispatcher>>();

        _mockUpdateOrderHandler.Setup(h => h.Name).Returns("UpdateOrder");
        _mockUpdateUserHandler.Setup(h => h.Name).Returns("UpdateUser");

        _config = new TopicSettings
        {
            CurrentSet = "Development",
            Sets = new Dictionary<string, List<TopicConfigEntry>>
            {
                ["Development"] = new List<TopicConfigEntry>
                {
                    new() { TopicName = "topic_1", EventType = "user.created", HandlerName = "UpdateUser" },
                    new() { TopicName = "topic_2", EventType = "order.created", HandlerName = "UpdateOrder" }
                }
            }
        };

        _mockConfigOptions = new Mock<IOptions<TopicSettings>>();
        _mockConfigOptions.Setup(x => x.Value).Returns(_config);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object, _mockUpdateUserHandler.Object };

        // Act
        var dispatcher = new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object);

        // Assert
        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void Constructor_WithInvalidCurrentSet_ThrowsInvalidOperationException()
    {
        // Arrange
        _config.CurrentSet = "NonExistentSet";
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object, _mockUpdateUserHandler.Object };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithMissingHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object }; // Missing UpdateUser handler

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object));
    }

    [Fact]
    public void DispatchEvent_WithValidEventType_DispatchesToCorrectHandler()
    {
        // Arrange
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object, _mockUpdateUserHandler.Object };
        var dispatcher = new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object);
        var cloudEvent = new CloudEvent
        {
            Type = "user.created",
            Id = "test-id"
        };

        _mockUpdateUserHandler.Setup(h => h.ProcessEvent(cloudEvent)).Returns(true);

        // Act
        var result = dispatcher.DispatchEvent(cloudEvent);

        // Assert
        Assert.True(result);
        _mockUpdateUserHandler.Verify(h => h.ProcessEvent(cloudEvent), Times.Once);
        _mockUpdateOrderHandler.Verify(h => h.ProcessEvent(It.IsAny<CloudEvent>()), Times.Never);
    }

    [Fact]
    public void DispatchEvent_WithUnknownEventType_ReturnsFalse()
    {
        // Arrange
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object, _mockUpdateUserHandler.Object };
        var dispatcher = new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object);
        var cloudEvent = new CloudEvent
        {
            Type = "unknown.event",
            Id = "test-id"
        };

        // Act
        var result = dispatcher.DispatchEvent(cloudEvent);

        // Assert
        Assert.False(result);
        _mockUpdateUserHandler.Verify(h => h.ProcessEvent(It.IsAny<CloudEvent>()), Times.Never);
        _mockUpdateOrderHandler.Verify(h => h.ProcessEvent(It.IsAny<CloudEvent>()), Times.Never);
    }

    [Fact]
    public void DispatchEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object, _mockUpdateUserHandler.Object };
        var dispatcher = new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dispatcher.DispatchEvent(null));
    }

    [Fact]
    public void DispatchEvent_WhenHandlerThrowsException_ReturnsFalse()
    {
        // Arrange
        var handlers = new List<IEventHandler> { _mockUpdateOrderHandler.Object, _mockUpdateUserHandler.Object };
        var dispatcher = new EventDispatcher(_mockConfigOptions.Object, handlers, _mockLogger.Object);
        var cloudEvent = new CloudEvent
        {
            Type = "user.created",
            Id = "test-id"
        };

        _mockUpdateUserHandler.Setup(h => h.ProcessEvent(cloudEvent))
            .Throws(new Exception("Test exception"));

        // Act
        var result = dispatcher.DispatchEvent(cloudEvent);

        // Assert
        Assert.False(result);
        _mockUpdateUserHandler.Verify(h => h.ProcessEvent(cloudEvent), Times.Once);
    }
} 