using Confluent.Kafka;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using KafkaConsumer.Features.UpdateUser.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace KafkaConsumer.Tests.Common.Services;

public class TopicResolverTests
{
    private readonly Mock<IOptions<TopicSettings>> _mockTopicConfig;
    private readonly Mock<ILogger<TopicResolver>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly TopicResolver _resolver;

    public TopicResolverTests()
    {
        _mockTopicConfig = new Mock<IOptions<TopicSettings>>();
        _mockLogger = new Mock<ILogger<TopicResolver>>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicConfigEntry>>
            {
                ["TestSet"] = new List<TopicConfigEntry>
                {
                    new() { TopicName = "test-topic", EventType = "test.event", HandlerName = "KafkaConsumer.Features.UpdateOrder.Handlers.UpdateOrderHandler" },
                    new() { TopicName = "wildcard-topic", EventType = "*", HandlerName = "KafkaConsumer.Features.UpdateUser.Handlers.UpdateUserHandler" }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);
        _resolver = new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object);
    }

    [Fact]
    public void ResolveHandler_WithExactMatch_ReturnsHandler()
    {
        // Arrange
        var mockHandler = new Mock<IEventHandler>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(UpdateOrderHandler)))
            .Returns(mockHandler.Object);

        var consumeResult = CreateConsumeResult("test-topic", "test.event");

        // Act
        var result = _resolver.ResolveHandler(consumeResult);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockHandler.Object, result);
    }

    [Fact]
    public void ResolveHandler_WithWildcardMatch_ReturnsHandler()
    {
        // Arrange
        var mockHandler = new Mock<IEventHandler>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(UpdateUserHandler)))
            .Returns(mockHandler.Object);

        var consumeResult = CreateConsumeResult("wildcard-topic", "test.something");

        // Act
        var result = _resolver.ResolveHandler(consumeResult);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockHandler.Object, result);
    }

    [Fact]
    public void ResolveHandler_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var consumeResult = CreateConsumeResult("unknown-topic", "unknown.event");

        // Act
        var result = _resolver.ResolveHandler(consumeResult);

        // Assert
        Assert.Null(result);
    }

    private static ConsumeResult<string, byte[]> CreateConsumeResult(string topic, string eventType)
    {
        var headers = new Headers();
        headers.Add("type", System.Text.Encoding.UTF8.GetBytes(eventType));

        return new ConsumeResult<string, byte[]>
        {
            Topic = topic,
            Message = new Message<string, byte[]>
            {
                Headers = headers,
                Value = System.Text.Encoding.UTF8.GetBytes("{\"key\":\"value\"}")
            },
        };
    }
}