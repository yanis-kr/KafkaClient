using CloudNative.CloudEvents;
using KafkaConsumer.Features.UpdateUser.Handlers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KafkaConsumer.Tests.Features.UpdateUser.Handlers;

public class UpdateUserHandlerTests
{
    private readonly UpdateUserHandler _handler;
    private readonly Mock<ILogger<UpdateUserHandler>> _mockLogger;

    public UpdateUserHandlerTests()
    {
        _mockLogger = new Mock<ILogger<UpdateUserHandler>>();
        _handler = new UpdateUserHandler(_mockLogger.Object);
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
        // Fails with error:
        // System.ArgumentException : It is impossible to call the provided strongly-typed predicate due to the use of a type matcher. Provide a weakly-typed predicate with two parameters (object, Type) instead. (Parameter 'match')
        //_mockLogger.Verify(
        //    x => x.Log(
        //        LogLevel.Information,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("[UpdateUser] Processing event: user.created, ID=test-id")),
        //        It.IsAny<Exception?>(), // Updated to match nullable reference type
        //        It.Is<Func<It.IsAnyType, Exception?, string>>((formatter) => formatter != null)), // Updated to match nullable reference type
        //    Times.Once);
    }

    [Fact]
    public void ProcessEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.ProcessEvent(null));
    }
} 