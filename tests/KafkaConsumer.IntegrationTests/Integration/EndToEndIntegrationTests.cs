using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Models;
using KafkaConsumer.Features.UpdateOrder.Contracts;
using KafkaConsumer.Features.UpdateOrder.Models;
using KafkaConsumer.IntegrationTests.Fixtures;
using KafkaConsumer.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KafkaConsumer.IntegrationTests.Integration;

public class EndToEndIntegrationTests : IClassFixture<MockedHttpKafkaConsumerTestFixture>
{
    private readonly MockedHttpKafkaConsumerTestFixture _fixture;
    
    public EndToEndIntegrationTests(MockedHttpKafkaConsumerTestFixture fixture)
    {
        _fixture = fixture;
        
        // Set up test token response
        var tokenResponse = new OktaTokenResponse 
        { 
            AccessToken = "end-to-end-test-token", 
            TokenType = "Bearer", 
            ExpiresIn = 3600,
            Scope = "default" 
        };
        
        // Configure mock to return token for Okta token requests
        _fixture.SetupMockJsonResponse(
            req => req.RequestUri.Host.Contains("okta.com") && req.RequestUri.PathAndQuery.Contains("/token"),
            JsonSerializer.Serialize(tokenResponse));
    }
    
    [Fact]
    public async Task EndToEnd_ProcessOrderUpdateEvent_ShouldCallOrderApi()
    {
        // Arrange - Set up the event
        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "e2e-test-order-123",
            OrderNumber = "ORD-E2E-FULL-123",
            Status = "Delivered",
            Amount = 149.95m,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        // Create the message
        var consumeResult = FakeKafkaMessageHelper.CreateFakeConsumeResult(orderEvent, "order-updates");
        
        // Get the services we need from DI
        var topicResolver = _fixture.Services.GetRequiredService<ITopicResolver>();
        
        // Clear any previous requests
        _fixture.CapturedRequests.Clear();
        
        // Act
        // Resolve handlers for this topic/message
        var handlers = topicResolver.ResolveHandlers(consumeResult).ToList();
        
        // Process with each resolved handler
        foreach (var handler in handlers)
        {
            await handler.ProcessEvent(consumeResult);
        }
        
        // Assert
        // Verify we have handlers
        Assert.NotEmpty(handlers);
        
        // Verify HTTP requests were made
        Assert.NotEmpty(_fixture.CapturedRequests);
        
        // Find the order update request
        var orderUpdateRequest = _fixture.CapturedRequests
            .FirstOrDefault(r => r.Method == HttpMethod.Put && r.RequestUri.PathAndQuery.Contains($"/api/orders/{orderEvent.OrderId}"));
        
        Assert.NotNull(orderUpdateRequest);
        
        // Check that the Authorization header was added with our token
        Assert.True(orderUpdateRequest.Headers.Authorization != null);
        Assert.Equal("Bearer", orderUpdateRequest.Headers.Authorization.Scheme);
        Assert.Equal("end-to-end-test-token", orderUpdateRequest.Headers.Authorization.Parameter);
        
        // Check the request body
        var requestContent = await orderUpdateRequest.Content.ReadAsStringAsync();
        var requestBody = JsonSerializer.Deserialize<OrderUpdateRequest>(requestContent);
        
        Assert.NotNull(requestBody);
        Assert.Equal(orderEvent.Status, requestBody.Status);
        Assert.Equal(orderEvent.OrderNumber, requestBody.OrderNumber);
        Assert.Equal(orderEvent.Amount, requestBody.Amount);
    }
    
    [Fact]
    public async Task EndToEnd_InvalidEvent_ShouldNotCallOrderApi()
    {
        // Arrange - Create an invalid message
        var consumeResult = FakeKafkaMessageHelper.CreateInvalidConsumeResult("order-updates");
        
        // Get the services we need from DI
        var topicResolver = _fixture.Services.GetRequiredService<ITopicResolver>();
        
        // Clear any previous requests
        _fixture.CapturedRequests.Clear();
        
        // Act
        // Resolve handlers for this topic/message
        var handlers = topicResolver.ResolveHandlers(consumeResult).ToList();
        
        // Process with each resolved handler
        foreach (var handler in handlers)
        {
            await handler.ProcessEvent(consumeResult);
        }
        
        // Assert
        // Verify we have handlers (we should have handlers for the topic)
        Assert.NotEmpty(handlers);
        
        // But we should have no HTTP requests (processing should fail validation)
        var orderApiRequests = _fixture.CapturedRequests
            .Where(r => r.Method == HttpMethod.Put && r.RequestUri.PathAndQuery.Contains("/api/orders/"));
            
        Assert.Empty(orderApiRequests);
    }
} 