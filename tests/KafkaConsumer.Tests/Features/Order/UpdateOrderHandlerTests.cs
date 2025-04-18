using Confluent.Kafka;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace KafkaConsumer.Tests.Features.Order;

public class UpdateOrderHandlerTests
{
    private readonly Mock<ILogger<UpdateOrderHandler>> _mockLogger;
    private readonly UpdateOrderHandler _handler;

    public UpdateOrderHandlerTests()
    {
        _mockLogger = new Mock<ILogger<UpdateOrderHandler>>();
        _handler = new UpdateOrderHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task ProcessEvent_WithJsonMessage_ProcessesSuccessfully()
    {
        // Arrange
        var orderUpdate = new OrderUpdate
        {
            OrderId = "123",
            Status = "Completed"
        };
        var jsonContent = JsonSerializer.Serialize(orderUpdate);
        var consumeResult = CreateConsumeResult("order-topic", "order.update", Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await _handler.ProcessEvent(consumeResult);

        // Assert
        Assert.True(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing order update: OrderId=123, Status=Completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_WithBinaryMessage_ProcessesSuccessfully()
    {
        // Arrange
        var binaryContent = new byte[] { 1, 2, 3, 4, 5 };
        var consumeResult = CreateConsumeResult("order-topic", "order.update", binaryContent);

        // Act
        var result = await _handler.ProcessEvent(consumeResult);

        // Assert
        Assert.True(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing binary message of length 5 bytes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_WithEmptyMessage_ReturnsFalse()
    {
        // Arrange
        var consumeResult = CreateConsumeResult("order-topic", "order.update", Array.Empty<byte>());

        // Act
        var result = await _handler.ProcessEvent(consumeResult);

        // Assert
        Assert.False(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Received empty message from topic order-topic")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_WithInvalidJson_HandlesGracefully()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var consumeResult = CreateConsumeResult("order-topic", "order.update", Encoding.UTF8.GetBytes(invalidJson));

        // Act
        var result = await _handler.ProcessEvent(consumeResult);

        // Assert - will be true but processed as binary
        Assert.True(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Message content (Binary)")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_WithOpenShiftLoggedContent_ProcessesSuccessfully()
    {
        // This test can be updated with actual content from OpenShift logs
        // Example: Copy the content from the log and paste it here
        var jsonContent = @"{""OrderId"":""456"",""Status"":""Processing""}";
        var consumeResult = CreateConsumeResult("order-topic", "order.update", Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await _handler.ProcessEvent(consumeResult);

        // Assert
        Assert.True(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing order update: OrderId=456, Status=Processing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvent_WithOpenShiftLoggedBinaryContent_ProcessesSuccessfully()
    {
        // This test can be updated with actual binary content from OpenShift logs
        // Example: Copy the base64 content from the log and convert it to bytes
        var base64Content = "AQIDBAU="; // This is the base64 representation of [1, 2, 3, 4, 5]
        var binaryContent = Convert.FromBase64String(base64Content);
        var consumeResult = CreateConsumeResult("order-topic", "order.update", binaryContent);

        // Act
        var result = await _handler.ProcessEvent(consumeResult);

        // Assert
        Assert.True(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing binary message of length 5 bytes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static ConsumeResult<string, byte[]> CreateConsumeResult(string topic, string eventType, byte[] value)
    {
        var headers = new Headers();
        headers.Add("type", Encoding.UTF8.GetBytes(eventType));

        return new ConsumeResult<string, byte[]>
        {
            Topic = topic,
            Partition = 0,
            Offset = 0,
            Message = new Message<string, byte[]>
            {
                Headers = headers,
                Value = value
            }
        };
    }
} 