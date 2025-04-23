using Confluent.Kafka;
using KafkaConsumer.Features.UpdateOrder.Contracts;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using KafkaConsumer.Features.UpdateOrder.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace KafkaConsumer.Tests.Features.UpdateOrder.Handlers;

public class UpdateOrderHandlerTests
{
    private readonly Mock<IOrderApi> _mockOrderApi;
    private readonly Mock<ILogger<UpdateOrderHandler>> _mockLogger;
    
    public UpdateOrderHandlerTests()
    {
        _mockOrderApi = new Mock<IOrderApi>();
        _mockLogger = new Mock<ILogger<UpdateOrderHandler>>();
    }
    
    [Fact]
    public async Task ProcessEvent_ShouldCallOrderApi_WhenValidMessageReceived()
    {
        // Arrange
        var handler = new UpdateOrderHandler(_mockOrderApi.Object, _mockLogger.Object);
        
        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "order-123",
            OrderNumber = "ORD-123",
            Status = "Shipped",
            Amount = 99.99m,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        var json = JsonSerializer.Serialize(orderEvent);
        var messageValue = Encoding.UTF8.GetBytes(json);
        
        var headers = new Headers();
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "order-123",
                Value = messageValue,
                Headers = headers
            },
            Topic = "order-updates",
            Partition = 0,
            Offset = 100
        };
        
        OrderUpdateRequest capturedRequest = null;
        string capturedOrderId = null;
        
        _mockOrderApi
            .Setup(api => api.UpdateOrderAsync(It.IsAny<string>(), It.IsAny<OrderUpdateRequest>()))
            .Callback<string, OrderUpdateRequest>((orderId, request) => 
            {
                capturedOrderId = orderId;
                capturedRequest = request;
            })
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.True(result);
        
        _mockOrderApi.Verify(
            api => api.UpdateOrderAsync(It.IsAny<string>(), It.IsAny<OrderUpdateRequest>()),
            Times.Once);
            
        Assert.Equal("order-123", capturedOrderId);
        Assert.Equal("Shipped", capturedRequest.Status);
        Assert.Equal("ORD-123", capturedRequest.OrderNumber);
        Assert.Equal(99.99m, capturedRequest.Amount);
    }
    
    [Fact]
    public async Task ProcessEvent_ShouldReturnFalse_WhenEmptyMessageReceived()
    {
        // Arrange
        var handler = new UpdateOrderHandler(_mockOrderApi.Object, _mockLogger.Object);
        
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "order-123",
                Value = Array.Empty<byte>(),
                Headers = new Headers()
            },
            Topic = "order-updates",
            Partition = 0,
            Offset = 100
        };
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.False(result);
        
        _mockOrderApi.Verify(
            api => api.UpdateOrderAsync(It.IsAny<string>(), It.IsAny<OrderUpdateRequest>()),
            Times.Never);
    }
    
    [Fact]
    public async Task ProcessEvent_ShouldReturnFalse_WhenNonJsonMessageReceived()
    {
        // Arrange
        var handler = new UpdateOrderHandler(_mockOrderApi.Object, _mockLogger.Object);
        
        var messageValue = Encoding.UTF8.GetBytes("This is not JSON");
        
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "order-123",
                Value = messageValue,
                Headers = new Headers()
            },
            Topic = "order-updates",
            Partition = 0,
            Offset = 100
        };
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.False(result);
        
        _mockOrderApi.Verify(
            api => api.UpdateOrderAsync(It.IsAny<string>(), It.IsAny<OrderUpdateRequest>()),
            Times.Never);
    }
    
    [Fact]
    public async Task ProcessEvent_ShouldReturnFalse_WhenApiThrowsException()
    {
        // Arrange
        var handler = new UpdateOrderHandler(_mockOrderApi.Object, _mockLogger.Object);
        
        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "order-123",
            OrderNumber = "ORD-123",
            Status = "Shipped",
            Timestamp = DateTimeOffset.UtcNow
        };
        
        var json = JsonSerializer.Serialize(orderEvent);
        var messageValue = Encoding.UTF8.GetBytes(json);
        
        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "order-123",
                Value = messageValue,
                Headers = new Headers()
            },
            Topic = "order-updates",
            Partition = 0,
            Offset = 100
        };
        
        _mockOrderApi
            .Setup(api => api.UpdateOrderAsync(It.IsAny<string>(), It.IsAny<OrderUpdateRequest>()))
            .ThrowsAsync(new Exception("API error"));
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.False(result);
        
        _mockOrderApi.Verify(
            api => api.UpdateOrderAsync(It.IsAny<string>(), It.IsAny<OrderUpdateRequest>()),
            Times.Once);
    }
} 