using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Features.UpdateOrder.Contracts;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using KafkaConsumer.Features.UpdateOrder.Models;
using KafkaConsumer.IntegrationTests.Fixtures;
using KafkaConsumer.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace KafkaConsumer.IntegrationTests.Integration;

[Collection("Integration tests")]
public class UpdateOrderHandlerTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    public UpdateOrderHandlerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        
        // Set up mock HTTP handler
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }
    
    [Fact]
    public async Task ProcessEvent_WithValidEvent_ShouldCallOrderApi()
    {
        // Arrange
        // Create a fake order update event
        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "integration-test-order-123",
            OrderNumber = "ORD-123-INT",
            Status = "Shipped",
            Amount = 199.99m,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        // Create a fake consume result
        var consumeResult = FakeKafkaMessageHelper.CreateFakeConsumeResult(orderEvent);
        
        // Set up the mock HTTP handler to return a success response
        HttpRequestMessage capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\": true}")
            });
        
        // Create a new service collection with our order API mock
        var services = new ServiceCollection();
        services.AddSingleton<IOrderApi>(_ => 
        {
            // We'll use a real client with our mock handler
            var client = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.example.com")
            };
            return Refit.RestService.For<IOrderApi>(client);
        });
        
        // Get the logger from the main fixture
        var logger = _fixture.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UpdateOrderHandler>>();
        services.AddSingleton(logger);
        
        // Build a service provider with our mocked services
        var serviceProvider = services.BuildServiceProvider();
        
        // Create a handler with our mocked services
        var orderApi = serviceProvider.GetRequiredService<IOrderApi>();
        var handler = new UpdateOrderHandler(orderApi, logger);
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.True(result);
        Assert.NotNull(capturedRequest);
        
        // Check the request was made to the correct endpoint
        Assert.Equal(HttpMethod.Put, capturedRequest.Method);
        Assert.Equal($"/api/orders/{orderEvent.OrderId}", capturedRequest.RequestUri.PathAndQuery);
        
        // Check the request body
        var requestContent = await capturedRequest.Content.ReadAsStringAsync();
        var requestBody = JsonSerializer.Deserialize<OrderUpdateRequest>(requestContent);
        
        Assert.NotNull(requestBody);
        Assert.Equal(orderEvent.Status, requestBody.Status);
        Assert.Equal(orderEvent.OrderNumber, requestBody.OrderNumber);
        Assert.Equal(orderEvent.Amount, requestBody.Amount);
    }
    
    [Fact]
    public async Task ProcessEvent_WithInvalidEvent_ShouldReturnFalse()
    {
        // Arrange
        // Create a non-JSON message
        var consumeResult = FakeKafkaMessageHelper.CreateInvalidConsumeResult();
        
        // Use the real handler from DI
        var handler = _fixture.ServiceProvider.GetRequiredService<UpdateOrderHandler>();
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task ProcessEvent_WithEmptyEvent_ShouldReturnFalse()
    {
        // Arrange
        // Create an empty message
        var consumeResult = FakeKafkaMessageHelper.CreateEmptyConsumeResult();
        
        // Use the real handler from DI
        var handler = _fixture.ServiceProvider.GetRequiredService<UpdateOrderHandler>();
        
        // Act
        var result = await handler.ProcessEvent(consumeResult);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task IntegrationTest_ResolveAndProcess_ShouldWorkEndToEnd()
    {
        // Arrange
        // Create a fake order update event
        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "end-to-end-test-order-123",
            OrderNumber = "ORD-E2E-123",
            Status = "Processing",
            Amount = 299.99m,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        // Create a fake consume result
        var consumeResult = FakeKafkaMessageHelper.CreateFakeConsumeResult(orderEvent, "order-updates");
        
        // Get topic resolver from DI
        var topicResolver = _fixture.ServiceProvider.GetRequiredService<ITopicResolver>();
        
        // Act
        // Resolve handlers for the message
        var handlers = topicResolver.ResolveHandlers(consumeResult).ToList();
        
        // Process with each handler
        var results = new List<bool>();
        foreach (var handler in handlers)
        {
            // Since our real API is mocked in the DI container, this won't make real HTTP calls
            var result = await handler.ProcessEvent(consumeResult);
            results.Add(result);
        }
        
        // Assert
        Assert.NotEmpty(handlers);
        Assert.NotEmpty(results);
        Assert.Contains(true, results); // At least one handler should succeed
    }
} 