using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Features.UpdateOrder.Models;
using KafkaConsumer.IntegrationTests.Fixtures;
using KafkaConsumer.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Xunit;

namespace KafkaConsumer.IntegrationTests.Integration;

public class TopicResolverTests : IClassFixture<KafkaConsumerTestFixture>
{
    private readonly KafkaConsumerTestFixture _fixture;
    private readonly ITopicResolver _topicResolver;

    public TopicResolverTests(KafkaConsumerTestFixture fixture)
    {
        _fixture = fixture;
        _topicResolver = _fixture.Services.GetRequiredService<ITopicResolver>();
    }

    [Fact]
    public void TopicResolver_ResolveHandlers_ShouldReturnCorrectHandlers()
    {
        // Arrange - Create a fake order update event
        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "test-order-123",
            OrderNumber = "ORD-123",
            Status = "Shipped",
            Amount = 99.99m,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Create a fake consume result with this event
        var consumeResult = FakeKafkaMessageHelper.CreateFakeConsumeResult(orderEvent, "order-updates");

        // Act - Resolve handlers for this message
        var handlers = _topicResolver.ResolveHandlers(consumeResult).ToList();

        // Assert - We should find at least one handler
        Assert.NotEmpty(handlers);
        
        // The handler should be of the correct type
        var handlerTypes = handlers.Select(h => h.GetType().FullName);
        Assert.Contains("KafkaConsumer.Features.UpdateOrder.Handlers.UpdateOrderHandler", handlerTypes);
    }

    [Fact]
    public void TopicResolver_ResolveHandlers_ShouldWorkWithEvents()
    {
        // Arrange - Create a consume result with CloudEvent format
        var cloudEventHeaders = new Headers
        {
            { "ce_id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
            { "ce_source", Encoding.UTF8.GetBytes("test-source") },
            { "ce_type", Encoding.UTF8.GetBytes("order.updated") },
            { "ce_specversion", Encoding.UTF8.GetBytes("1.0") },
            { "content-type", Encoding.UTF8.GetBytes("application/json") }
        };

        var orderEvent = new UpdateOrderEvent
        {
            OrderId = "test-order-456",
            OrderNumber = "ORD-456",
            Status = "Processed",
            Amount = 149.99m,
            Timestamp = DateTimeOffset.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(orderEvent);
        var messageValue = Encoding.UTF8.GetBytes(json);

        var consumeResult = new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = "test-order-456",
                Value = messageValue,
                Headers = cloudEventHeaders,
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = "order-updates",
            Partition = new Partition(0),
            Offset = new Offset(1)
        };

        // Act - Resolve handlers for this message
        var handlers = _topicResolver.ResolveHandlers(consumeResult).ToList();

        // Assert - We should find at least one handler
        Assert.NotEmpty(handlers);
    }
} 